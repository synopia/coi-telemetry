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
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.ComputingPower;
using Mafi.Core.Factory.ElectricPower;
using Mafi.Core.Maintenance;
using Mafi.Core.Population;
using Mafi.Base;

namespace CoiTelemetry.RealMod.Collecting;

public abstract class BaseMetrics
{
    protected readonly IModContext Context;
    protected readonly EntityTracker Tracker;
    private readonly IProductFlowMetrics _flowMetrics;
    private readonly Entity _entity;
    private bool _hasObservedStep;
    private int _lastObservedStepValue;
    private int _nextObservationStepValue;
    
    protected int ObservedTicks { get; set; }
    protected double ObservedSeconds => ObservedTicks * SimStep.SECONDS_PER_STEP;
    protected int SampleTicks { get; private set; } = 1;
    private readonly Dictionary<ObservedState, int> _stateCounters = new();
    protected double? Maintenance { get; set; }
    protected double? Power { get; set; }
    protected double? Computing { get; set; }
    protected double? Workers { get; set; }
    protected int Electricity { get; set; }
    
    protected CargoMetrics Usage { get; } = new();
    private readonly Dictionary<ProductId, double> _producedByProduct = new();
    private readonly Dictionary<ProductId, double> _consumedByProduct = new();
    
    protected BaseMetrics(IModContext context,IProductFlowMetrics flowMetrics, EntityTracker tracker, Entity entity)
    {
        Context = context;
        _flowMetrics = flowMetrics;
        Tracker = tracker;
        _entity = entity;
    }

    public bool ShouldUpdate(SimStep currentStep)
    {
        return !_hasObservedStep || currentStep.Value >= _nextObservationStepValue;
    }

    protected void TrackState(ObservedState state)
    {
        _stateCounters.TryGetValue(state, out var ticks);
        _stateCounters[state] = ticks + SampleTicks;
    }
    
    protected abstract void ObserveState();
    protected virtual int RecommendNextUpdateTicks() => 1;

    protected virtual void AfterObserveState()
    {
        using (SimProfiler.Scope("SwapBuffers"))
        {
            Usage.SwapBuffers();
        }
        
        foreach (var kv in Usage.GetDelta())
        {
            if (kv.Value < 0)
            {
                using (SimProfiler.Scope("AddConsumed"))
                {
                    AddConsumed(kv.Key, -kv.Value);
                }
            }
        }
    }

    public void Update(SimStep currentStep)
    {
        using (SimProfiler.Scope("ObserveAt"))
        {
            ObserveAt(currentStep);
        }
    }

    public void Flush(SimStep currentStep)
    {
        if (_hasObservedStep && _lastObservedStepValue == currentStep.Value)
        {
            return;
        }

        ObserveAt(currentStep);
    }

    private void ObserveAt(SimStep currentStep)
    {
        SampleTicks = !_hasObservedStep
            ? 1
            : Math.Max(1, currentStep.Value - _lastObservedStepValue);
        _hasObservedStep = true;
        _lastObservedStepValue = currentStep.Value;
        ObservedTicks += SampleTicks;

        if (!_entity.IsEnabled)
        {
            Power = null;
            Computing = null;
            Maintenance = null;
            Workers = null;
            Electricity = 0;
            return; 
        }
        if (_entity is IMaintainedEntity maintainedEntity)
        {
            using (SimProfiler.Scope("ObserveMaintenance"))
            {
                var maintenanceStatus = maintainedEntity.Maintenance.Status;
                var maintenanceMax = maintenanceStatus.MaintenancePointsMax.Value.ToDouble();
                Maintenance = maintenanceMax <= 0
                    ? null
                    : 1-Math.Min(1, maintenanceStatus.MaintenancePointsCurrent.Value.ToDouble() / maintenanceMax);
                if (!maintainedEntity.Maintenance.CanWork())
                {
                    TrackState(ObservedState.NotEnoughMaintenance);
                }
                
                var costs = maintainedEntity.MaintenanceCosts;
                AddConsumed(Tracker.Ids.Product(costs.Product.Id), costs.MaintenancePerMonth.Value.ToDouble()/Duration.OneMonth.Ticks);
            }
        }

        if (_entity is IEntityWithFuelTank fuelTankEntity)
        {
            using (SimProfiler.Scope("ObserveFuel"))
            {
                var fuelTank = fuelTankEntity.FuelTank.ValueOrNull;
                if (fuelTank is not null)
                {
                    var maxDurationTicks = fuelTank.Proto.OneQuantityDuration.Ticks * fuelTank.Proto.Capacity.Value;
                    Power = maxDurationTicks <= 0
                        ? 0
                        : 1-(double)fuelTank.RemainingDuration.Ticks / maxDurationTicks;
                    if (((FuelTank)fuelTank).IsEmpty)
                    {
                        TrackState(ObservedState.NotEnoughPower);
                    }

                    Usage.Set(Tracker.Product(fuelTank.Proto.Product),
                        (double)fuelTank.RemainingDuration.Ticks / fuelTank.Proto.OneQuantityDuration.Ticks);
                }
            }
        }
        
        if (_entity is IElectricityConsumingEntity electricityConsumingEntity)
        {
            using (SimProfiler.Scope("ObservePower"))
            {
                var power = (ElectricityConsumer?)electricityConsumingEntity.ElectricityConsumer.ValueOrNull;
                if (power is not null)
                {
                    if (!power.NotEnoughPower)
                    {
                        Power = 0;
                        Electricity = power.PowerRequired.Value;
                    }
                    else
                    {
                        Power = 1;
                        Electricity = 0;
                        TrackState(ObservedState.NotEnoughPower);
                    }
                }
            }
        }

        if (_entity is IComputingConsumingEntity computingConsumingEntity)
        {
            using (SimProfiler.Scope("ObserveComputing"))
            {
                var computing = computingConsumingEntity.ComputingConsumer.ValueOrNull;
                Computing = computing is null
                    ? null
                    : computing.ComputingRequired.Value <= 0
                        ? null
                        : 1-Math.Min(1, (double)computing.ComputingCharged.Value / computing.ComputingRequired.Value);
                if (computing?.NotEnoughComputing == true)
                {
                    TrackState(ObservedState.NotEnoughComputing);
                }

                if (Computing is not null)
                {
                    Usage.Set(Tracker.Ids.Product(Ids.Products.Computing), (double)Computing);
                }
            }
        }

        if (_entity is IEntityWithWorkers workersEntity)
        {
            using (SimProfiler.Scope("ObserveWorkers"))
            {
                
                Workers = workersEntity.WorkersNeeded <= 0
                    ? null
                    : 1-(double)workersEntity.WorkersAssigned() / workersEntity.WorkersNeeded;
                
                if (Workers > 0)
                {
                    TrackState(ObservedState.NotEnoughWorkers);
                }
            }
        }

        using (SimProfiler.Scope("ObserveState"))
        {
            ObserveState();
        }

        using (SimProfiler.Scope("AfterObserveState"))
        {
            AfterObserveState();
        }

        _nextObservationStepValue = currentStep.Value + Math.Max(1, RecommendNextUpdateTicks());
        SampleTicks = 1;
    }

    public virtual void ResetWindow()
    {
        ObservedTicks = 0;
        Maintenance = null;
        Power = null;
        Computing = null;
        Workers = null;
        Electricity = 0;
        _producedByProduct.Clear();
        _consumedByProduct.Clear();

        _stateCounters.Clear();
    }
    
    public void AddProduced(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }
        AddTo(_producedByProduct, productId, amount);
        _flowMetrics.AddProduced(productId, amount);
    }
    public void AddConsumed(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }
        AddTo(_consumedByProduct, productId, amount);
        _flowMetrics.AddConsumed(productId, amount);
    }
    
    protected static void AddTo(Dictionary<ProductId, double> dict, ProductId productId, double amount)
    {
        dict.TryGetValue(productId, out var current);
        dict[productId] = current + amount;
    }
    
    protected ObservedState GetPrimaryBlocker()
    {
        var filtered  = _stateCounters.Where(s=>s.Key!=ObservedState.Working&&s.Key!=ObservedState.Idle).ToArray();
        if (filtered.Length == 0)
        {
            return ObservedState.Unknown;
        }
        var best = filtered.OrderByDescending(x => x.Value).First();
        return best.Value <= 0 ? ObservedState.Unknown : best.Key;
    }

    protected ProductFlowSummary[] BuildProduceFlow() =>
        _producedByProduct
            .Select(x => new ProductFlowSummary(
                ProductId: x.Key.Value,
                Amount: x.Value,
                PerMinute: MetricMath.PerMinute(x.Value, ObservedSeconds)))
            .OrderBy(x => x.ProductId)
            .ToArray();
    
    protected ProductFlowSummary[] BuildConsumeFlow() =>
        _consumedByProduct
            .Select(x => new ProductFlowSummary(
                ProductId: x.Key.Value,
                Amount: x.Value,
                PerMinute: MetricMath.PerMinute(x.Value, ObservedSeconds)))
            .OrderBy(x => x.ProductId)
            .ToArray();
    
    protected Dictionary<ObservedState, int> BuildStateCounters() => _stateCounters.ToDictionary(x => x.Key, x => x.Value);
    protected Dictionary<ObservedState, double> BuildStatePercentages() => MetricMath.Percent(_stateCounters, ObservedTicks);
}
