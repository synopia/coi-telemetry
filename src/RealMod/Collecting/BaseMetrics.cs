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
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.ComputingPower;
using Mafi.Core.Factory.ElectricPower;
using Mafi.Core.Maintenance;
using Mafi.Core.Population;
using Mafi.Core.Products;
using Mafi.Base;

namespace CoiTelemetry.RealMod.Collecting;

public abstract class BaseMetrics
{
    protected readonly IModContext Context;
    protected readonly EntityTracker Tracker;
    private readonly IProductFlowMetrics _flowMetrics;
    private readonly Entity Entity;
    
    protected int ObservedTicks { get; set; }
    protected double ObservedSeconds => ObservedTicks * SimStep.SECONDS_PER_STEP;
    private readonly Dictionary<ObservedState, int> _stateCounters = new();
    protected double Maintenance { get; set; }
    protected double Power { get; set; }
    protected double Computing { get; set; }
    protected double Workers { get; set; }
   
    protected CargoMetrics Usage { get; } = new();
    private readonly Dictionary<ProductId, double> _producedByProduct = new();
    private readonly Dictionary<ProductId, double> _consumedByProduct = new();
    
    protected BaseMetrics(IModContext context,IProductFlowMetrics flowMetrics, EntityTracker tracker, Entity entity)
    {
        Context = context;
        _flowMetrics = flowMetrics;
        Tracker = tracker;
        Entity = entity;
    }

    protected void TrackState(ObservedState state)
    {
        _stateCounters.TryGetValue(state, out var ticks);
        _stateCounters[state] = ticks + 1;
    }
    
    protected abstract void ObserveState();

    protected virtual void AfterObserveState()
    {
        Usage.SwapBuffers();
        foreach (var kv in Usage.GetDelta())
        {
            if (kv.Value < 0)
            {
                AddConsumed(kv.Key, -kv.Value);
            }
        }
    }
    public void Update()
    {
        ObservedTicks++;
        if (Entity is IMaintainedEntity maintainedEntity)
        {
            var maintenanceStatus = maintainedEntity.Maintenance.Status;
            Maintenance = Math.Min(1, maintenanceStatus.MaintenancePointsCurrent.Value.ToDouble() /
                                      maintenanceStatus.MaintenancePointsMax.Value.ToDouble());
            if (!maintainedEntity.Maintenance.CanWork())
            {
                TrackState(ObservedState.NotEnoughMaintenance);
            }
            Usage.Set(Tracker.Ids.Product(Ids.Products.MaintenanceT1), Maintenance);
        }

        if (Entity is IEntityWithFuelTank fuelTankEntity)
        {
            var fuelTank = fuelTankEntity.FuelTank.ValueOrNull;
            if (fuelTank is not null)
            {
                Power =  (double)fuelTank.RemainingDuration.Ticks/(fuelTank.Proto.OneQuantityDuration.Ticks*fuelTank.Proto.Capacity.Value);
                if (((FuelTank)fuelTank).IsEmpty)
                {
                    TrackState(ObservedState.NotEnoughPower);
                }
                Usage.Set(Tracker.Product(fuelTank.Proto.Product), (double)fuelTank.RemainingDuration.Ticks/fuelTank.Proto.OneQuantityDuration.Ticks);
            }
        }
        
        if (Entity is IElectricityConsumingEntity electricityConsumingEntity)
        {
            var power =(ElectricityConsumer?) electricityConsumingEntity.ElectricityConsumer.ValueOrNull;
            if (power is not null)
            {
                if (!power.NotEnoughPower)
                {
                    Power = 1;
                    AddConsumed(Tracker.Ids.Product(Ids.Products.Electricity), power.PowerRequired.Value);
                }
                else
                {
                    Power = 0;
                    TrackState(ObservedState.NotEnoughPower);
                }
                
            }
        }

        if (Entity is IComputingConsumingEntity computingConsumingEntity)
        {
            var computing = computingConsumingEntity.ComputingConsumer.ValueOrNull;
            Computing = computing is null ? 0 : Math.Min(1, (double)computing.ComputingCharged.Value / computing.ComputingRequired.Value);
            if (computing?.NotEnoughComputing == true)
            {
                TrackState(ObservedState.NotEnoughComputing);
            }
            Usage.Set(Tracker.Ids.Product(Ids.Products.Computing), Computing);
        }

        if (Entity is IEntityWithWorkers workersEntity)
        {
            Workers = (double)workersEntity.WorkersAssigned()/workersEntity.WorkersNeeded;
            if (Workers < 1)
            {
                TrackState(ObservedState.NotEnoughWorkers);
            }
        }

        ObserveState();

        AfterObserveState();
    }

    public virtual void ResetWindow()
    {
        ObservedTicks = 0;
        Maintenance = 0;
        Power = 0;
        Computing = 0;
        Workers = 0;
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