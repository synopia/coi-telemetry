using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Enums;
using CoiTelemetry.RealMod.Contracts.Ids;
using CoiTelemetry.RealMod.Mapping;
using Mafi;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Products;

namespace CoiTelemetry.RealMod.Collecting;

public sealed class MachineMetrics
{
    private readonly EntityTracker _tracker;
    private readonly IProductFlowMetrics _productFlowMetrics;
    private readonly Machine _machine;
    
    private readonly EntityId _machineId;
    private int _observedTicks;
    private MachineObservedState _observedState;
    private RecipeId? _recipeId;
    private readonly Dictionary<MachineObservedState, int> _stateCounters = new();
    private readonly Dictionary<ProductId, double> _inputConsumed = new();
    private readonly Dictionary<ProductId, double> _outputProduced = new();
    
    public MachineMetrics(EntityTracker tracker, IProductFlowMetrics productFlowMetrics, Machine machine)
    {
        _tracker = tracker;
        _productFlowMetrics = productFlowMetrics;
        _machine = machine;
        
        _machineId = _tracker.Machine(machine);
    }

    public void ObserveState()
    {
        _observedTicks++;
        _observedState = GetState();
        
        _stateCounters.TryGetValue(_observedState, out var ticks);
        _stateCounters[_observedState] = ticks + 1;
        
        var recipe = _machine.LastRecipeInProgress.ValueOrNull;
        
        if (recipe is not null)
        {
            _recipeId = _tracker.Recipe(recipe);
            if(_machine is {WorkedThisTick:true, RecipeProductionTicks:{Ticks:0}})
            {
                recipe.AllInputs.ForEach(input =>
                {
                    var id = _tracker.Product(input.Product);
                    AddInputConsumed(id, input.Quantity.Value);
                    _productFlowMetrics.AddConsumed(id, input.Quantity.Value);
                });
                recipe.AllOutputs.ForEach(output =>
                {
                    var id = _tracker.Product(output.Product);
                    AddOutputProduced(id, output.Quantity.Value);
                    _productFlowMetrics.AddProduced(id, output.Quantity.Value);
                });
            }
        }
    }

    public MachineSummaryRow BuildSummaryRow()
    {
        var windowSeconds = SimStep.SECONDS_PER_STEP * _observedTicks;

        var inputFlows = _inputConsumed
            .Select(x => new ProductFlowSummary(
                ProductId: x.Key.Value,
                Amount: x.Value,
                PerMinute: MetricMath.PerMinute(x.Value, windowSeconds)))
            .OrderBy(x => x.ProductId)
            .ToArray();

        var outputFlows = _outputProduced
            .Select(x=>new ProductFlowSummary(
                ProductId:x.Key.Value,
                Amount:x.Value,
                PerMinute:MetricMath.PerMinute(x.Value, windowSeconds)))
            .OrderBy(x=>x.ProductId)
            .ToArray();

        var inputBuffers = _inputConsumed
            .Select(x => BuildProductBuffer(_machine, x.Key))
            .OrderBy(x => x.ProductId)
            .ToArray();

        var outputBuffers = _outputProduced
            .Select(x => BuildProductBuffer(_machine, x.Key))
            .OrderBy(x => x.ProductId)
            .ToArray();

        return new MachineSummaryRow(
            MachineId: _machineId.Value,
            RecipeId: _recipeId?.Value,
            ObservedTicks: _observedTicks,
            UptimePercent: MetricMath.Percent(_stateCounters, _observedTicks),
            UptimeTicks: _stateCounters.ToDictionary(x => x.Key, x => x.Value),
            Inputs: inputFlows,
            Outputs: outputFlows,
            InputBuffers: inputBuffers,
            OutputBuffers: outputBuffers,
            PrimaryBlocker: GetPrimaryBlocker()
        );
    }
    
    
    private static ProductBufferSummary BuildProductBuffer(Machine machine, ProductId productId)
    {
        var product = machine.Context.ProtosDb.Get<ProductProto>(productId.CoiId).Value;
        if (product == null)
        {
            return new ProductBufferSummary(productId.Value, 0, 0, 0);
        }

        var capacity = machine.GetInputCapacityFor(product).Value;
        var stored = machine.GetInputQuantityFor(product).Value;
        var fillPercent = capacity<=0 ? 0 : stored * 100.0 / capacity;
        return new ProductBufferSummary(ProductId:productId.Value, Capacity:capacity, Stored:stored, FillPercent:fillPercent);
    }
    public void ResetWindow()
    {
        _observedTicks = 0;
        _observedState = MachineObservedState.None;

        _stateCounters.Clear();

        _inputConsumed.Clear();
        _outputProduced.Clear();
    }
    private MachineObservedState GetState()
    {
        switch (_machine.CurrentState)
        {
            case Machine.State.Paused:
                return MachineObservedState.Paused;
            case Machine.State.Broken:
                return MachineObservedState.Broken;
            case Machine.State.NotEnoughWorkers:
                return MachineObservedState.NotEnoughWorkers;
            case Machine.State.NotEnoughPower:
                return MachineObservedState.NotEnoughPower;
            case Machine.State.NotEnoughComputing:
                return MachineObservedState.NotEnoughComputing;
            case Machine.State.NotEnoughInput:
                return MachineObservedState.NotEnoughInput;
            case Machine.State.InvalidPlacement:
                return MachineObservedState.InvalidPlacement;
            case Machine.State.OutputFull:
                return MachineObservedState.OutputFull;
            case Machine.State.NoRecipes:
                return MachineObservedState.NoRecipes;
            case Machine.State.Working:
                return MachineObservedState.Working;
            default:
                return MachineObservedState.None;
        }
    }
    private MachineObservedState GetPrimaryBlocker()
    {
        var best = _stateCounters.OrderByDescending(x => x.Value).First();
        return best.Value <= 0 ? MachineObservedState.None : best.Key;
    }
    private void AddInputConsumed(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }

        AddTo(_inputConsumed, productId, amount);
    }

    private void AddOutputProduced(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }

        AddTo(_outputProduced, productId, amount);
    }
    
    
    private static void AddTo(Dictionary<ProductId, double> dict, ProductId productId, double amount)
    {
        dict.TryGetValue(productId, out var current);
        dict[productId] = current + amount;
    }
}