using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Enums;
using CoiTelemetry.RealMod.Contracts.Ids;
using CoiTelemetry.RealMod.Mapping;
using Mafi;
using Mafi.Core;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.PathFinding.Goals;
using Mafi.Core.Population;
using Mafi.Core.Prototypes;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.Jobs;
using Mafi.Core.Vehicles.TreeHarvesters;
using Mafi.Core.Vehicles.Trucks;
using EntityId = CoiTelemetry.RealMod.Contracts.Ids.EntityId;

namespace CoiTelemetry.RealMod.Collecting;

public abstract class VehicleMetrics : BaseMetrics
{
    public static VehicleMetrics Create(IModContext context, EntityTracker tracker,
        IProductFlowMetrics productFlowMetrics, Vehicle vehicle)
    {
        if (vehicle is Truck truck)
        {
            return new TruckMetrics(context,  productFlowMetrics,tracker, truck);
        }

        if (vehicle is Excavator excavator)
        {
            return new ExcavatorMetrics(context, productFlowMetrics, tracker, excavator);
        }
        
        if (vehicle is TreeHarvester treeHarvester)
        {
            return new TreeHarvesterMetrics(context, productFlowMetrics, tracker, treeHarvester);
        }
        
        throw new ArgumentException($"Unsupported vehicle type: {vehicle.GetType().Name}", nameof(vehicle));
    }
    
    private readonly Vehicle _vehicle;
    private readonly EntityId _vehicleId;
    
    protected EntityId? AssignedToId {get; private set;}
    protected string? CurrentGoal { get; set;}
    protected int DeliveriesCompleted { get; set; }

    private double _distanceEmpty;
    private double _distanceLoaded;
    private long _totalDistance;
    
    private readonly CargoMetrics _cargo = new();
    private readonly Dictionary<ProductId, double> _deliveredByProduct = new();
    
    private readonly Dictionary<string, int> _jobsInfo = new();
    
    protected bool IsDelivering { get; init; } = false;
    protected bool IsProducing { get; init; } = false;

    public VehicleMetrics(IModContext context, IProductFlowMetrics productFlowMetrics, EntityTracker tracker, Vehicle vehicle):base(context, productFlowMetrics, tracker, vehicle)
    {
        _vehicle = vehicle;
        _vehicleId = Tracker.Vehicle(vehicle);

        _totalDistance = vehicle.LifetimeDistanceTraveled.RawValue;
    }

    protected void UpdateCargo(IVehicleCargo cargo)
    {
        var it = cargo.GetEnumerator();
        while (it.MoveNext())
        {
            var productId = Tracker.Product(it.Current.Key);
            _cargo.Set(productId, it.Current.Value.Value);
        }
    }


    protected abstract ObservedState FindState();
    
    protected override void ObserveState()
    {
        var assignedTo = _vehicle.AssignedTo.ValueOrNull;
        AssignedToId = Tracker.Entity(assignedTo);
        var goal = _vehicle.NavigationGoal.ValueOrNull;
        CurrentGoal = goal?.GoalName.Value;

        var totalDistance = _vehicle.LifetimeDistanceTraveled.RawValue;
        AddMovedDistance(totalDistance - _totalDistance, !_cargo.IsEmpty);
        _totalDistance = totalDistance;

        var currentJob = _vehicle.CurrentJob.ValueOrNull;
        // if (currentJob is not null)
        // {
        //     var msg = $"{currentJob.GetType()}({currentJob.JobInfo.Value})";
        //     _jobsInfo.TryGetValue(msg, out var count);
        //     _jobsInfo[msg] = count + 1;
        // }
        
        TrackState(FindState());
    }

    protected override void AfterObserveState()
    {
        base.AfterObserveState();
        
        _cargo.SwapBuffers();
        foreach (var kv in _cargo.GetDelta())
        {
            if (kv.Value<0)
            {
                if(IsDelivering)
                {
                    AddDeliveryCompleted(kv.Key, -kv.Value);
                    if (_cargo.GetLast(kv.Key) <= 0)
                    {
                        DeliveriesCompleted++;
                    }
                }
            }
            else 
            {
                if (IsProducing)
                {
                    AddProduced(kv.Key, kv.Value);
                }
            }            
        }
    }

    public override void ResetWindow()
    {
        base.ResetWindow();

        _jobsInfo.Clear();
        DeliveriesCompleted = 0;
        _distanceEmpty = 0;
        _distanceLoaded = 0;
        
        _deliveredByProduct.Clear();
    }
    
    public VehicleSummaryRow BuildSummaryRow()
    {
        var delivered = _deliveredByProduct
            .Select(x => new ProductFlowSummary(
                ProductId: x.Key.Value,
                Amount: x.Value,
                PerMinute: MetricMath.PerMinute(x.Value, ObservedSeconds)))
            .OrderBy(x => x.ProductId)
            .ToArray();

        return new VehicleSummaryRow(
            VehicleId: _vehicleId.Value,
            AssignedTo: AssignedToId?.Value,
            ObservedTicks: ObservedTicks,
            Maintenance: Maintenance,
            Power: Power,
            Computing: Computing,
            Workers: Workers,
            UptimePercent: BuildStatePercentages(),
            UptimeTicks: BuildStateCounters(),
            DeliveriesCompleted: DeliveriesCompleted,
            EmptyTravelDistance: _distanceEmpty,
            LoadedTravelDistance: _distanceLoaded,
            Jobs: _jobsInfo.ToDictionary(x => x.Key, x => x.Value),
            Delivered: delivered,
            Produced: BuildProduceFlow(),
            Consumed: BuildConsumeFlow(),
            PrimaryBlocker: GetPrimaryBlocker()
        );
    }

    protected void AddMovedDistance(double distance, bool loaded)
    {
        if (distance <= 0)
        {
            return;
        }

        if (loaded)
        {
            _distanceLoaded += distance;
        }
        else
        {
            _distanceEmpty += distance;
        }
    }

    protected void UpdateCargo(ProductQuantity productQuantity)
    {
        var productId = Tracker.Product(productQuantity.Product);
        _cargo.Set(productId, productQuantity.Quantity.Value);
    }

    protected void AddDeliveryCompleted(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }
        AddTo(_deliveredByProduct, productId, amount);
    }
}

public sealed class TruckMetrics : VehicleMetrics
{
    private Truck _truck;
    
    public TruckMetrics(IModContext context,  IProductFlowMetrics productFlowMetrics,EntityTracker tracker, Truck truck) : base(context, productFlowMetrics, tracker, truck)
    {
        _truck = truck;
        IsDelivering = true;
    }


    protected override ObservedState FindState()
    {
        UpdateCargo(_truck.Cargo);

        if (AssignedToId is not null)
        {
            if( _truck is { IsDriving: false, IsFull: true } )
            {
                // truck is assigned, full cargo and is not driving 
                // so it waits to deliver cargo
                return ObservedState.OutputFull;
            }
            // all other cases the truck is counted as working
            return ObservedState.Working;
        }

        
        if (_truck.IsDriving)
        {
            return ObservedState.Working;
        }

        if (_truck.NeedsJob)
        {
            return ObservedState.Idle;
        }
        
        if (_truck.Cargo.TotalQuantity.Value > 0)
        {
            return ObservedState.OutputFull;
        }

        return ObservedState.NotEnoughInput;
    }
}

public sealed class ExcavatorMetrics : VehicleMetrics
{
    private Excavator _excavator;
    public ExcavatorMetrics(IModContext context, IProductFlowMetrics productFlowMetrics, EntityTracker tracker, Excavator excavator) : base(context, productFlowMetrics, tracker, excavator)
    {
        _excavator = excavator;
        IsProducing = true;
    }

    protected override ObservedState FindState()
    {
        UpdateCargo(_excavator.Cargo);
        
        switch (_excavator.State)
        {
            case ExcavatorState.Idle:
                return ObservedState.Idle;
            case ExcavatorState.DoJob:
                return ObservedState.Working;
            case ExcavatorState.LoadTruck:
                return ObservedState.Working;
            case ExcavatorState.WaitingForShovel:
                return ObservedState.Working;
            case ExcavatorState.WaitingForTruck:
                return ObservedState.OutputFull;
            case ExcavatorState.GettingUnstuck:
                return ObservedState.Idle;
            default:
                return ObservedState.Unknown;
        }    
    }
}

public sealed class TreeHarvesterMetrics : VehicleMetrics
{
    private TreeHarvester _treeHarvester;

    public TreeHarvesterMetrics(IModContext context, IProductFlowMetrics productFlowMetrics, EntityTracker tracker,
        TreeHarvester treeHarvester) : base(context, productFlowMetrics, tracker, treeHarvester)
    {
        _treeHarvester = treeHarvester;
        IsProducing = true;
    }

    protected override ObservedState FindState()
    {
        UpdateCargo(_treeHarvester.Cargo);
        switch (_treeHarvester.State)
        {
            case TreeHarvesterState.TreeIsUp:
                return ObservedState.OutputFull;
            case TreeHarvesterState.Idle:
                return ObservedState.NotEnoughInput;
            default:
                return ObservedState.Working;
        }
    }
}