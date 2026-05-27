using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Enums;
using CoiTelemetry.RealMod.Contracts.Ids;
using CoiTelemetry.RealMod.Mapping;
using Mafi;
using Mafi.Core.Entities;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;

namespace CoiTelemetry.RealMod.Collecting;

public class MachineMetrics : BaseMetrics
{
    private readonly Machine _machine;
    private readonly EntityId _machineId;
    
    protected RecipeId? RecipeId { get; private set; }
    private RecipeProto? _recipeProto;
    
    
    public MachineMetrics(IModContext context, EntityTracker tracker, IProductFlowMetrics productFlowMetrics,Machine machine):base(context, productFlowMetrics,tracker, machine)
    {
        _machine = machine;
        _machineId = Tracker.Machine(machine);
    }

    protected override void ObserveState()
    {
        var recipe = _machine.LastRecipeInProgress.ValueOrNull;
        
        if (recipe is not null)
        {
            RecipeId = Tracker.Recipe(recipe);
            _recipeProto = recipe;
            if(_machine is {WorkedThisTick:true, ProgressPerc.IsNearHundred: true })
            {
                recipe.AllInputs.ForEach(input =>
                {
                    var id = Tracker.Product(input.Product);
                    AddConsumed(id, input.Quantity.Value);
                });
                recipe.AllOutputs.ForEach(output =>
                {
                    var id = Tracker.Product(output.Product);
                    AddProduced(id, output.Quantity.Value);
                });
            }
        }
        TrackState(FindState());
        
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
            Maintenance: Maintenance,
            Power: Power,
            Computing: Computing,
            Workers: Workers,
            Inputs: BuildConsumeFlow(),
            Outputs: BuildProduceFlow(),
            InputBuffers: inputBuffers,
            OutputBuffers: outputBuffers,
            PrimaryBlocker: GetPrimaryBlocker()
        );
    }
    
    
    private ProductBufferSummary BuildProductBuffer(Machine machine, ProductProto product, bool isInput = true)
    {
        var id = Tracker.Product(product);
        var capacity = isInput ? machine.GetInputCapacityFor(product).Value : machine.GetOutputCapacityFor(product).Value;
        var stored = isInput ? machine.GetInputQuantityFor(product).Value : machine.GetOutputQuantityFor(product).Value;
        var fillPercent = capacity<=0 ? 0 : stored * 100.0 / capacity;
        return new ProductBufferSummary(ProductId:id.Value, Capacity:capacity, Stored:stored, FillPercent:fillPercent);
    }
    public override void ResetWindow()
    {
        base.ResetWindow();

        RecipeId = null;
        _recipeProto = null;
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