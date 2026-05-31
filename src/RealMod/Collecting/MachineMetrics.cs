using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Enums;
using CoiTelemetry.RealMod.Contracts.Ids;
using CoiTelemetry.RealMod.Mapping;
using CoiTelemetry.RealMod.Runtime;
using Mafi;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;

namespace CoiTelemetry.RealMod.Collecting;

public class MachineMetrics : BaseMetrics
{
    private const double Epsilon = 0.0001;
    private readonly Machine _machine;
    private readonly EntityId _machineId;
    
    protected RecipeId? RecipeId { get; private set; }
    private RecipeProto? _recipeProto;
    private RecipeProto? _lastObservedRecipe;
    private int _lastRecipeProgressTicks;
    private bool _lastWorkedThisTick;
    private bool _hasCycleObservation;
    
    
    public MachineMetrics(IModContext context, MetaTracker tracker, IProductFlowMetrics productFlowMetrics,Machine machine):base(context, productFlowMetrics,tracker, machine)
    {
        _machine = machine;
        _machineId = Tracker.Entity(machine.Id);
    }

    protected override void ObserveState()
    {
        var recipe = _machine.LastRecipeInProgress.ValueOrNull;
        RecipeId = Tracker.Recipe(recipe.Id);
        _recipeProto = recipe;
        
        TrackState(FindState());
        TrackRecipeCycle(recipe);
    }

    public MachineSummaryRow BuildSummaryRow()
    {
        var inputBuffers = _recipeProto is not null ? _recipeProto.AllInputs
            .Select(x => BuildProductBuffer(_machine, x.Product, true))
            .OrderBy(x => x.ProductId)
            .ToArray() : Array.Empty<ProductBufferSummary>();

        var outputBuffers = _recipeProto is not null ? _recipeProto.AllOutputs
            .Select(x => BuildProductBuffer(_machine, x.Product, false))
            .OrderBy(x => x.ProductId)
            .ToArray() : Array.Empty<ProductBufferSummary>();

        return new MachineSummaryRow(
            MachineId: _machineId.Value,
            RecipeId: RecipeId?.Value,
            ObservedTicks: ObservedTicks,
            UptimePercent: BuildStatePercentages(),
            UptimeTicks: BuildStateCounters(),
            Electricity: Electricity,
            Pressure:new PressureSummary(Maintenance, Power, Computing, Workers),
            Inputs: BuildConsumeFlow(),
            Outputs: BuildProduceFlow(),
            InputBuffers: inputBuffers,
            OutputBuffers: outputBuffers,
            PotentialScenarios: BuildPotentialScenarios(),
            PrimaryBlocker: GetPrimaryBlocker()
        );
    }
    
    
    private ProductBufferSummary BuildProductBuffer(Machine machine, ProductProto product, bool isInput = true)
    {
        var id = Tracker.Product(product.Id);
        var capacity = isInput ? machine.GetInputCapacityFor(product).Value : machine.GetOutputCapacityFor(product).Value;
        var stored = isInput ? machine.GetInputQuantityFor(product).Value : machine.GetOutputQuantityFor(product).Value;
        var fillPercent = capacity<=0 ? 0 : stored * 100.0 / capacity;
        return new ProductBufferSummary(ProductId:id.Value, Capacity:capacity, Stored:stored, FillPercent:fillPercent);
    }

    private void TrackRecipeCycle(RecipeProto? recipe)
    {
        var progressTicks = _machine.RecipeProductionTicks.Ticks;
        var workedThisTick = _machine.WorkedThisTick;

        if (_hasCycleObservation && recipe is not null && DidStartRecipeCycle(recipe, progressTicks, workedThisTick))
        {
            RecordRecipeCycle(recipe);
        }

        _hasCycleObservation = true;
        _lastObservedRecipe = recipe;
        _lastRecipeProgressTicks = progressTicks;
        _lastWorkedThisTick = workedThisTick;
    }

    private bool DidStartRecipeCycle(RecipeProto recipe, int progressTicks, bool workedThisTick)
    {
        if (!workedThisTick)
        {
            return false;
        }

        if (_machine.GetTargetDurationFor(recipe).Ticks <= 1)
        {
            return true;
        }

        if (!ReferenceEquals(recipe, _lastObservedRecipe))
        {
            return true;
        }

        if (!_lastWorkedThisTick)
        {
            return true;
        }

        return progressTicks <= _lastRecipeProgressTicks;
    }

    private void RecordRecipeCycle(RecipeProto recipe)
    {
        using (SimProfiler.Scope("RecordRecipeCycle"))
        {
            var utilization = _machine.Utilization;

            foreach (var input in recipe.AllInputs)
            {
                var consumed = input.Quantity.ScaledBy(utilization);
                if (consumed.IsPositive)
                {
                    AddConsumed(Tracker.Product(input.Product.Id), consumed.Value);
                }
            }

            foreach (var output in recipe.OutputsAtStart)
            {
                var produced = output.Quantity.ScaledBy(utilization);
                if (produced.IsPositive)
                {
                    AddProduced(Tracker.Product(output.Product.Id), produced.Value);
                }
            }

            foreach (var output in recipe.OutputsAtEnd)
            {
                var produced = output.Quantity.ScaledBy(utilization);
                if (output.Product.Type == VirtualProductProto.ProductType)
                {
                    produced = produced.ScaledBy(_machine.VirtualOutputMultiplier);
                }

                if (produced.IsPositive)
                {
                    AddProduced(Tracker.Product(output.Product.Id), produced.Value);
                }
            }
        }
    }

    private MachinePotentialScenario[] BuildPotentialScenarios()
    {
        if (_recipeProto is null)
        {
            return Array.Empty<MachinePotentialScenario>();
        }

        var cycleDuration = _machine.GetTargetDurationFor(_recipeProto);
        if (cycleDuration.IsNotPositive)
        {
            return Array.Empty<MachinePotentialScenario>();
        }

        var factors = new Dictionary<string, double>
        {
            ["maintenance"] = GetResourceFactor(Maintenance),
            ["power"] = GetResourceFactor(Power),
            ["computing"] = GetResourceFactor( Computing),
            ["workers"] = GetResourceFactor( Workers),
        };

        var scenarios = new List<MachinePotentialScenario>();
        var currentFactor = ComputeCapacityFactor(factors);

        if (currentFactor < 1 - Epsilon)
        {
            scenarios.Add(BuildPotentialScenario(
                "current-local-capacity",
                "Current local staffing and utilities",
                currentFactor,
                cycleDuration));
        }

        AddFixScenario(scenarios, factors, "maintenance", "Fix maintenance", cycleDuration, currentFactor);
        AddFixScenario(scenarios, factors, "power", "Fix power", cycleDuration, currentFactor);
        AddFixScenario(scenarios, factors, "computing", "Fix computing", cycleDuration, currentFactor);
        AddFixScenario(scenarios, factors, "workers", "Fix workers", cycleDuration, currentFactor);

        scenarios.Add(BuildPotentialScenario(
            "full-local-capacity",
            "All local bottlenecks fixed",
            1,
            cycleDuration));

        return scenarios.ToArray();
    }

    private void AddFixScenario(
        List<MachinePotentialScenario> scenarios,
        Dictionary<string, double> factors,
        string factorToIgnore,
        string label,
        Duration cycleDuration,
        double currentFactor)
    {
        if (factors[factorToIgnore] >= 1 - Epsilon)
        {
            return;
        }

        var factor = ComputeCapacityFactor(factors, factorToIgnore);
        if (factor <= currentFactor + Epsilon)
        {
            return;
        }

        scenarios.Add(BuildPotentialScenario(
            $"fix-{factorToIgnore}",
            label,
            factor,
            cycleDuration));
    }

    private MachinePotentialScenario BuildPotentialScenario(
        string scenarioId,
        string label,
        double factor,
        Duration cycleDuration)
    {
        var inputs = _recipeProto?.AllInputs
            .Select(input => BuildPotentialFlow(input.Product, input.Quantity.Value, cycleDuration, factor))
            .OrderBy(x => x.ProductId)
            .ToArray() ?? Array.Empty<ProductFlowSummary>();

        var outputs = _recipeProto?.AllOutputs
            .Select(output => BuildPotentialFlow(output.Product, output.Quantity.Value, cycleDuration, factor))
            .OrderBy(x => x.ProductId)
            .ToArray() ?? Array.Empty<ProductFlowSummary>();

        return new MachinePotentialScenario(
            ScenarioId: scenarioId,
            Label: label,
            Factor: factor,
            Inputs: inputs,
            Outputs: outputs);
    }

    private ProductFlowSummary BuildPotentialFlow(ProductProto product, double quantityPerCycle, Duration cycleDuration, double factor)
    {
        var productId = Tracker.Product(product.Id);
        var cycleSeconds = cycleDuration.Seconds.ToDouble();
        var perMinuteAtFullCapacity = MetricMath.PerMinute(quantityPerCycle, cycleSeconds);
        var perMinute = perMinuteAtFullCapacity * factor;
        var amount = perMinute * ObservedSeconds / 60.0;
        return new ProductFlowSummary(
            ProductId: productId.Value,
            Amount: amount,
            PerMinute: perMinute);
    }

    private static double GetResourceFactor(double? value)
    {
        return value is null
            ? 1
            : Math.Max(0, Math.Min(1, (double)value));
    }

    private static double ComputeCapacityFactor(IReadOnlyDictionary<string, double> factors, string? ignoredFactor = null)
    {
        var factor = 1.0;
        foreach (var kv in factors)
        {
            if (kv.Key == ignoredFactor)
            {
                continue;
            }

            factor = Math.Min(factor, kv.Value);
        }

        return factor;
    }

    private ObservedState FindState()
    {
        switch (_machine.CurrentState)
        {
            case Machine.State.Paused:
            case Machine.State.Broken:
            case Machine.State.InvalidPlacement:
            case Machine.State.NoRecipes:
                return ObservedState.Idle;
            case Machine.State.NotEnoughWorkers:
                return ObservedState.NotEnoughWorkers;
            case Machine.State.NotEnoughPower:
                return ObservedState.NotEnoughPower;
            case Machine.State.NotEnoughComputing:
                return ObservedState.NotEnoughComputing;
            case Machine.State.NotEnoughInput:
                return ObservedState.NotEnoughInput;
            case Machine.State.OutputFull:
                return ObservedState.OutputFull;
            case Machine.State.Working:
                return ObservedState.Working;
            default:
                return ObservedState.Unknown;
        }
    }
}
