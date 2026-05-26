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
using Mafi.Core.Prototypes;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.Jobs;
using Mafi.Core.Vehicles.TreeHarvesters;
using Mafi.Core.Vehicles.Trucks;
using EntityId = CoiTelemetry.RealMod.Contracts.Ids.EntityId;

namespace CoiTelemetry.RealMod.Collecting;

public abstract class VehicleMetrics
{
    public static VehicleMetrics Create(IModContext context, EntityTracker tracker,
        IProductFlowMetrics productFlowMetrics, Vehicle vehicle)
    {
        if (vehicle is Truck truck)
        {
            return new TruckMetrics(context, tracker, productFlowMetrics, truck);
        }

        if (vehicle is Excavator excavator)
        {
            return new ExcavatorMetrics(context, tracker, productFlowMetrics, excavator);
        }
        
        if (vehicle is TreeHarvester treeHarvester)
        {
            return new TreeHarvesterMetrics(context, tracker, productFlowMetrics, treeHarvester);
        }
        
        throw new ArgumentException($"Unsupported vehicle type: {vehicle.GetType().Name}", nameof(vehicle));
    }
    
    private readonly EntityTracker _tracker;
    private readonly IProductFlowMetrics _productFlowMetrics;
    private readonly Vehicle _vehicle;
    
    private readonly EntityId _vehicleId;
    private int _observedTicks;
    private ObservedState _observedState;
    private readonly Dictionary<ObservedState, int> _stateCounters = new();

    private EntityId? _assignedToId;
    private string? _currentGoal;
    private int _deliveriesCompleted;
    private double _fuelConsumed;
    private double _distanceEmpty;
    private double _distanceLoaded;

    private long _totalDistance;
    private readonly CargoMetrics _cargo = new();
    private readonly CargoMetrics _usage = new();
    
    private readonly Dictionary<ProductId, double> _deliveredByProduct = new();
    private readonly Dictionary<ProductId, double> _producedByProduct = new();
    private readonly Dictionary<ProductId, double> _consumedByProduct = new();
    private readonly Dictionary<string, int> _jobsInfo = new();
    private readonly IModContext _context;
    
    public bool IsDelivering { get; set; } = false;
    public bool IsProducing { get; set; } = false;

    public VehicleMetrics(IModContext context, EntityTracker tracker, IProductFlowMetrics productFlowMetrics, Vehicle vehicle)
    {
        _context = context;
        _tracker = tracker;
        _productFlowMetrics = productFlowMetrics;
        _vehicle = vehicle;
        _vehicleId = _tracker.Vehicle(vehicle);

        _totalDistance = vehicle.LifetimeDistanceTraveled.RawValue;
    }

    protected abstract ObservedState ObserveState(IEntityAssignedWithVehicles? assignedTo, IVehicleGoal? goal);

    protected void UpdateFuel()
    {
        var fuelTank = _vehicle.FuelTank.ValueOrNull;
        if (fuelTank is not null)
        {
            _usage.Set(_tracker.Product(fuelTank.Proto.Product), (double)fuelTank.RemainingDuration.Ticks/fuelTank.Proto.OneQuantityDuration.Ticks);

            // var fuelTicks = fuelTank.RemainingDuration;
            // var diff = _lastFuel - fuelTicks;
            // var tankTicks= fuelTank.Proto.OneQuantityDuration.Ticks*fuelTank.Proto.Capacity.Value;
            // var currentFuel = (double)diff.Ticks/tankTicks;
            // AddFuelConsumed(currentFuel);
            // _lastFuel = fuelTicks;
        }
    }

    protected void UpdateCargo(IVehicleCargo cargo)
    {
        var it = cargo.GetEnumerator();
        while (it.MoveNext())
        {
            var productId = _tracker.Product(it.Current.Key);
            _cargo.Set(productId, it.Current.Value.Value);
        }
    }

    protected void UpdateCargo(ProductQuantity productQuantity)
    {
        var productId = _tracker.Product(productQuantity.Product);
        _cargo.Set(productId, productQuantity.Quantity.Value);
    }

    public void ObserveState()
    {
        _observedTicks++;
        var assignedTo = _vehicle.AssignedTo.ValueOrNull;
        _assignedToId = _tracker.Entity(assignedTo);
        var goal = _vehicle.NavigationGoal.ValueOrNull;
        _currentGoal = goal?.GoalName.Value;
        
        UpdateFuel();
        ObservedState state = ObservedState.Unknown;
        
        if (_vehicle.CannotWorkDueToLowFuel)
        {
            state = ObservedState.NotEnoughPower;
        }

        if (_vehicle.IsStuck)
        {
            state = ObservedState.Waiting;
        }

        if (!_vehicle.Maintenance.CanWork())
        {
            state = ObservedState.NotEnoughMaintenance;
        }

        if (state == ObservedState.Unknown)
        {
            state = ObserveState(assignedTo, goal);
        }

        var totalDistance = _vehicle.LifetimeDistanceTraveled.RawValue;
        AddMovedDistance(totalDistance-_totalDistance, !_cargo.IsEmpty);
        _totalDistance = totalDistance;
        
        var currentJob = _vehicle.CurrentJob.ValueOrNull;
        // if (currentJob is not null)
        // {
        //     var msg = $"{currentJob.GetType()}({currentJob.JobInfo.Value})";
        //     _jobsInfo.TryGetValue(msg, out var count);
        //     _jobsInfo[msg] = count + 1;
        // }
        _cargo.SwapBuffers();
        _usage.SwapBuffers();
        foreach (var kv in _cargo.GetDelta())
        {
            if (kv.Value<0)
            {
                if(IsDelivering)
                {
                    AddDeliveryCompleted(kv.Key, -kv.Value);
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

        foreach (var kv in _usage.GetDelta())
        {
            if (kv.Value < 0)
            {
                AddConsumed(kv.Key, -kv.Value);
            }
        }
        _stateCounters.TryGetValue(state, out var ticks);
        _stateCounters[state] = ticks + 1;
        _observedState = state;
        
  /*
        ObservedState state;
        if (_vehicle is Excavator excavator)
        {
            state = GetExcavatorObservedState(excavator);
            if (state != _observedState && state == ObservedState.Unloading)
            {
            }
        }
        else
        {
            if (_vehicle is IVehicleForCargoJob vehicleForCargoJob)
            {
                var currentCargo = vehicleForCargoJob.Cargo.TotalQuantity.Value;
                _cargoDelta = currentCargo - _lastCargoAmount;
                _lastCargoAmount = currentCargo;
            }
            state = _jobMetric?.Process(_vehicle) ?? GetVehicleState(); 
        }
        */
        
      
    }

    public void ResetWindow()
    {
        _observedTicks = 0;
        _stateCounters.Clear();
        _jobsInfo.Clear();
        _deliveriesCompleted = 0;
        _distanceEmpty = 0;
        _distanceLoaded = 0;
        _fuelConsumed = 0;
        
        _deliveredByProduct.Clear();
        _producedByProduct.Clear();
        _consumedByProduct.Clear();
    }
    private ObservedState GetPrimaryBlocker()
    {
        if (_stateCounters.Count == 0)
        {
            return ObservedState.Unknown;
        }
        var best = _stateCounters.OrderByDescending(x => x.Value).First();
        return best.Value <= 0 ? ObservedState.Unknown : best.Key;
    }
    public VehicleSummaryRow BuildSummaryRow()
    {
        var windowSeconds = SimStep.SECONDS_PER_STEP * _observedTicks;
        var delivered = _deliveredByProduct
            .Select(x => new ProductFlowSummary(
                ProductId: x.Key.Value,
                Amount: x.Value,
                PerMinute: MetricMath.PerMinute(x.Value, windowSeconds)))
            .OrderBy(x => x.ProductId)
            .ToArray();

        var produced = _producedByProduct
            .Select(x => new ProductFlowSummary(
                ProductId: x.Key.Value,
                Amount: x.Value,
                PerMinute: MetricMath.PerMinute(x.Value, windowSeconds)))
            .OrderBy(x => x.ProductId)
            .ToArray();

        var consumed = _consumedByProduct
            .Select(x => new ProductFlowSummary(
                ProductId: x.Key.Value,
                Amount: x.Value,
                PerMinute: MetricMath.PerMinute(x.Value, windowSeconds)))
            .OrderBy(x => x.ProductId)
            .ToArray();
        return new VehicleSummaryRow(
            VehicleId: _vehicleId.Value,
            AssignedTo: _assignedToId?.Value,
            ObservedTicks: _observedTicks,
            UptimePercent:MetricMath.Percent(_stateCounters, _observedTicks),
            UptimeTicks: _stateCounters.ToDictionary(x => x.Key, x => x.Value),
            DeliveriesCompleted: _deliveriesCompleted,
            FuelConsumed: _fuelConsumed,
            EmptyTravelDistance: _distanceEmpty,
            LoadedTravelDistance: _distanceLoaded,
            Jobs: _jobsInfo.ToDictionary(x => x.Key, x => x.Value),
            Delivered: delivered,
            Produced: produced,
            Consumed: consumed,
            PrimaryBlocker: GetPrimaryBlocker()
        );
    }
    public void AddProduced(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }
        AddTo(_producedByProduct, productId, amount);
        _productFlowMetrics.AddProduced(productId, amount);
    }
    public void AddConsumed(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }
        AddTo(_consumedByProduct, productId, amount);
        _productFlowMetrics.AddConsumed(productId, amount);
    }

    public void AddMovedDistance(double distance, bool loaded)
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

    public void AddDeliveryCompleted(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }
        _deliveriesCompleted++;
        AddTo(_deliveredByProduct, productId, amount);
    }
    
    private static void AddTo(Dictionary<ProductId, double> dict, ProductId productId, double amount)
    {
        dict.TryGetValue(productId, out var current);
        dict[productId] = current + amount;
    }
  
}

public sealed class TruckMetrics : VehicleMetrics
{
    private Truck _truck;
    public TruckMetrics(IModContext context, EntityTracker tracker, IProductFlowMetrics productFlowMetrics, Truck truck) : base(context, tracker, productFlowMetrics, truck)
    {
        _truck = truck;
        IsDelivering = true;
    }


    protected override ObservedState ObserveState(IEntityAssignedWithVehicles? assignedTo, IVehicleGoal? goal)
    {
        UpdateCargo(_truck.Cargo);

        if (assignedTo is not null)
        {
            if (goal is StaticEntityVehicleGoal staticGoal)
            {
                var staticEntity = staticGoal.GoalStaticEntity.ValueOrNull;
                if (staticEntity is not null)
                {
                    if( _truck is { IsDriving: false, IsNotEmpty: true } )
                    {
                        // truck is assigned, has cargo, is not driving and current goal is static entity
                        // so it waits to deliver cargo
                        return ObservedState.OutputFull;
                    }
                }
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
            return ObservedState.Waiting;
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
    public ExcavatorMetrics(IModContext context, EntityTracker tracker, IProductFlowMetrics productFlowMetrics, Excavator excavator) : base(context, tracker, productFlowMetrics, excavator)
    {
        _excavator = excavator;
        IsProducing = true;
    }

    protected override ObservedState ObserveState(IEntityAssignedWithVehicles? assignedTo, IVehicleGoal? goal)
    {
        UpdateCargo(_excavator.Cargo);
        
        switch (_excavator.State)
        {
            case ExcavatorState.Idle:
                return ObservedState.Waiting;
            case ExcavatorState.DoJob:
                return ObservedState.Working;
            case ExcavatorState.LoadTruck:
                return ObservedState.Working;
            case ExcavatorState.WaitingForShovel:
                return ObservedState.Working;
            case ExcavatorState.WaitingForTruck:
                return ObservedState.OutputFull;
            case ExcavatorState.GettingUnstuck:
                return ObservedState.Waiting;
            default:
                return ObservedState.Unknown;
        }    
    }
}

public sealed class TreeHarvesterMetrics : VehicleMetrics
{
    private TreeHarvester _treeHarvester;

    public TreeHarvesterMetrics(IModContext context, EntityTracker tracker, IProductFlowMetrics productFlowMetrics,
        TreeHarvester treeHarvester) : base(context, tracker, productFlowMetrics, treeHarvester)
    {
        _treeHarvester = treeHarvester;
        IsProducing = true;
    }

    protected override ObservedState ObserveState(IEntityAssignedWithVehicles? assignedTo, IVehicleGoal? goal)
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