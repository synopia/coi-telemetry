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
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.TreeHarvesters;
using Mafi.Core.Vehicles.Trucks;
using EntityId = CoiTelemetry.RealMod.Contracts.Ids.EntityId;

namespace CoiTelemetry.RealMod.Collecting;

public abstract class VehicleMetrics : BaseMetrics
{
    public static VehicleMetrics Create(IModContext context, MetaTracker tracker,
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
        
        return new GenericVehicleMetrics(context, productFlowMetrics, tracker, vehicle);
    }
    
    private readonly Vehicle _vehicle;
    private readonly EntityId _vehicleId;
    protected Vehicle TrackedVehicle => _vehicle;
    
    protected EntityId? AssignedToId {get; private set;}
    protected string? CurrentGoal { get; set;}
    protected string? CurrentJobType { get; private set; }
    protected string? CurrentJobInfo { get; private set; }
    protected int DeliveriesCompleted { get; set; }

    private double _distanceEmpty;
    private double _distanceLoaded;
    private long _totalDistance;
    
    private readonly CargoMetrics _cargo = new();
    private readonly Dictionary<ProductId, double> _deliveredByProduct = new();
    private readonly Dictionary<VehicleBlockerKind, int> _blockerCounters = new();
    
    private readonly Dictionary<string, int> _jobsInfo = new();
    private ObservedState _lastObservedState;
    
    protected bool IsDelivering { get; init; } = false;
    protected bool IsProducing { get; init; } = false;

    public VehicleMetrics(IModContext context, IProductFlowMetrics productFlowMetrics, MetaTracker tracker, Vehicle vehicle):base(context, productFlowMetrics, tracker, vehicle)
    {
        _vehicle = vehicle;
        _vehicleId = Tracker.Entity(vehicle.Id);

        _totalDistance = vehicle.LifetimeDistanceTraveled.RawValue;
    }

    protected void UpdateCargo(IVehicleCargo cargo)
    {
        var it = cargo.GetEnumerator();
        while (it.MoveNext())
        {
            var productId = Tracker.Product(it.Current.Key.Id);
            _cargo.Set(productId, it.Current.Value.Value);
        }
    }


    protected abstract ObservedState FindState();
    protected virtual VehicleBlockerKind FindBlocker(ObservedState state)
    {
        using (SimProfiler.Scope("FindBlocker"))
        {


            if (TrackedVehicle.CannotWorkDueToLowFuel || TrackedVehicle.NeedsRefueling)
            {
                return TrackedVehicle.LastRefuelRequestIssue switch
                {
                    RefuelRequestIssue.FailedAsUnreachable => VehicleBlockerKind.RefuelUnreachable,
                    RefuelRequestIssue.Failed => VehicleBlockerKind.RefuelRequestFailed,
                    _ => VehicleBlockerKind.NeedsFuel,
                };
            }

            if (TrackedVehicle.IsStuck)
            {
                return VehicleBlockerKind.Stuck;
            }

            if (TrackedVehicle.IsStrugglingToNavigate)
            {
                return VehicleBlockerKind.StrugglingToNavigate;
            }

            if (TrackedVehicle.PfState == PathFindingEntityState.PathFinding)
            {
                return VehicleBlockerKind.PathFinding;
            }

            if (TrackedVehicle.PfState == PathFindingEntityState.WaitingForRoadExit)
            {
                return VehicleBlockerKind.WaitingForRoadExit;
            }

            if (TrackedVehicle.NavigationFailed || TrackedVehicle.UnreachableGoal.HasValue)
            {
                return VehicleBlockerKind.GoalUnreachable;
            }

            return state switch
            {
                ObservedState.NotEnoughMaintenance => VehicleBlockerKind.NotEnoughMaintenance,
                ObservedState.NotEnoughWorkers => VehicleBlockerKind.NotEnoughWorkers,
                ObservedState.NotEnoughComputing => VehicleBlockerKind.NotEnoughComputing,
                ObservedState.Idle when TrackedVehicle.NeedsJob => VehicleBlockerKind.NoJob,
                _ => VehicleBlockerKind.None,
            };
        }
    }

    private void TrackBlocker(VehicleBlockerKind blocker)
    {
        using (SimProfiler.Scope("TrackBlocker"))
        {
            if (blocker == VehicleBlockerKind.None)
            {
                return;
            }

            _blockerCounters.TryGetValue(blocker, out var ticks);
            _blockerCounters[blocker] = ticks + SampleTicks;
        }
    }
    
    protected override void ObserveState()
    {
        var assignedTo = _vehicle.AssignedTo.ValueOrNull;
        AssignedToId = assignedTo!=null ? Tracker.Entity(assignedTo.Id) : null;
        var goal = _vehicle.NavigationGoal.ValueOrNull;
        CurrentGoal = goal?.GoalName.Value;

        var totalDistance = _vehicle.LifetimeDistanceTraveled.RawValue;
        AddMovedDistance(totalDistance - _totalDistance, !_cargo.IsEmpty);
        _totalDistance = totalDistance;

        var currentJob = _vehicle.CurrentJob.ValueOrNull;
        CurrentJobType = currentJob?.GetType().Name;
        CurrentJobInfo = string.IsNullOrWhiteSpace(TrackedVehicle.CurrentJobInfo.Value)
            ? null
            : TrackedVehicle.CurrentJobInfo.Value;
        if (currentJob is not null)
        {
            var jobKey = CurrentJobType ?? currentJob.GetType().Name;
            _jobsInfo.TryGetValue(jobKey, out var count);
            _jobsInfo[jobKey] = count + SampleTicks;
        }

        ObservedState state;
        using (SimProfiler.Scope("FindState"))
        {
            state = FindState();
        }
        _lastObservedState = state;
        using (SimProfiler.Scope("TrackState"))
        {
            TrackState(state);
        }

        using (SimProfiler.Scope("Track and FindBlocker"))
        {
            TrackBlocker(FindBlocker(state));
        }
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
        _blockerCounters.Clear();
        CurrentGoal = null;
        CurrentJobType = null;
        CurrentJobInfo = null;
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
            UptimePercent: BuildStatePercentages(),
            UptimeTicks: BuildStateCounters(),
            BlockerPercent: MetricMath.Percent(_blockerCounters, ObservedTicks),
            BlockerTicks: _blockerCounters.ToDictionary(x => x.Key, x => x.Value),
            Electricity: Electricity,
            Pressure: new PressureSummary(Maintenance, Power, Computing, Workers),
            DeliveriesCompleted: DeliveriesCompleted,
            EmptyTravelDistance: _distanceEmpty,
            LoadedTravelDistance: _distanceLoaded,
            Jobs: _jobsInfo.ToDictionary(x => x.Key, x => x.Value),
            Delivered: delivered,
            Produced: BuildProduceFlow(),
            Consumed: BuildConsumeFlow(),
            CurrentJob: CurrentJobType,
            CurrentJobInfo: CurrentJobInfo,
            CurrentGoal: CurrentGoal,
            PathFindingState: TrackedVehicle.PfState.ToString(),
            DrivingState: TrackedVehicle.DrivingState.ToString(),
            PrimaryDetailedBlocker: GetPrimaryBlockerKind(),
            PrimaryBlocker: GetPrimaryBlocker()
        );
    }

    private VehicleBlockerKind GetPrimaryBlockerKind()
    {
        var best = _blockerCounters
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();

        return best.Value <= 0
            ? VehicleBlockerKind.None
            : best.Key;
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
        var productId = Tracker.Product(productQuantity.Product.Id);
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

    protected override int RecommendNextUpdateTicks()
    {
        if (_lastObservedState != ObservedState.Working)
        {
            return 1;
        }

        if (TrackedVehicle.CannotWorkDueToLowFuel
            || TrackedVehicle.NeedsRefueling
            || TrackedVehicle.IsStuck
            || TrackedVehicle.IsStrugglingToNavigate
            || TrackedVehicle.NavigationFailed
            || TrackedVehicle.UnreachableGoal.HasValue
            || TrackedVehicle.PfState == PathFindingEntityState.PathFinding
            || TrackedVehicle.PfState == PathFindingEntityState.WaitingForRoadExit)
        {
            return 1;
        }

        if (!TrackedVehicle.IsDriving)
        {
            return 1;
        }

        return TrackedVehicle.DrivingState == DrivingState.DrivingForwardsOnRoad
            ? 5
            : 3;
    }
}

public sealed class TruckMetrics : VehicleMetrics
{
    private Truck _truck;
    
    public TruckMetrics(IModContext context,  IProductFlowMetrics productFlowMetrics,MetaTracker tracker, Truck truck) : base(context, productFlowMetrics, tracker, truck)
    {
        _truck = truck;
        IsDelivering = true;
    }


    protected override ObservedState FindState()
    {
        UpdateCargo(_truck.Cargo);

        if (AssignedToId is not null)
        {
            if (!_truck.IsDriving && _truck.IsFull)
            {
                // truck is assigned, full cargo and is not driving 
                // so it waits to deliver cargo
                return ObservedState.OutputFull;
            }

            if (!_truck.IsDriving && _truck.Cargo.TotalQuantity.Value <= 0)
            {
                return ObservedState.NotEnoughInput;
            }

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

    protected override VehicleBlockerKind FindBlocker(ObservedState state)
    {
        if (_truck.IsCannotDeliverNotificationActive)
        {
            return VehicleBlockerKind.CannotDeliverCargo;
        }

        return state switch
        {
            ObservedState.NotEnoughInput => VehicleBlockerKind.WaitingForPickup,
            ObservedState.OutputFull => VehicleBlockerKind.WaitingForUnload,
            _ => base.FindBlocker(state),
        };
    }
}

public sealed class ExcavatorMetrics : VehicleMetrics
{
    private Excavator _excavator;
    public ExcavatorMetrics(IModContext context, IProductFlowMetrics productFlowMetrics, MetaTracker tracker, Excavator excavator) : base(context, productFlowMetrics, tracker, excavator)
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

    protected override VehicleBlockerKind FindBlocker(ObservedState state)
    {
        if (_excavator.State == ExcavatorState.WaitingForTruck)
        {
            if (_excavator.TruckQueue.TrucksCount == 0)
            {
                return VehicleBlockerKind.NoTruckAvailable;
            }

            return VehicleBlockerKind.WaitingForTruck;
        }

        return base.FindBlocker(state);
    }
}

public sealed class TreeHarvesterMetrics : VehicleMetrics
{
    private TreeHarvester _treeHarvester;

    public TreeHarvesterMetrics(IModContext context, IProductFlowMetrics productFlowMetrics, MetaTracker tracker,
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

    protected override VehicleBlockerKind FindBlocker(ObservedState state)
    {
        if (_treeHarvester.Cargo.Quantity.IsPositive && _treeHarvester.VehiclesTotal() == 0)
        {
            return VehicleBlockerKind.NoTruckAvailable;
        }

        if (_treeHarvester.State == TreeHarvesterState.TreeIsUp)
        {
            return VehicleBlockerKind.WaitingForTruck;
        }

        if (_treeHarvester.State == TreeHarvesterState.Idle && _treeHarvester.DidNotFindTreeToHarvest)
        {
            return VehicleBlockerKind.NoHarvestTarget;
        }

        return base.FindBlocker(state);
    }
}

public sealed class GenericVehicleMetrics : VehicleMetrics
{
    public GenericVehicleMetrics(
        IModContext context,
        IProductFlowMetrics productFlowMetrics,
        MetaTracker tracker,
        Vehicle vehicle) : base(context, productFlowMetrics, tracker, vehicle)
    {
    }

    protected override ObservedState FindState()
    {
        if (AssignedToId is not null || TrackedVehicle.IsDriving || TrackedVehicle.CurrentJob.ValueOrNull is not null)
        {
            return ObservedState.Working;
        }

        if (TrackedVehicle.NeedsJob)
        {
            return ObservedState.Idle;
        }

        return ObservedState.Unknown;
    }
}
