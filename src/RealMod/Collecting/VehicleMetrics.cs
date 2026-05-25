using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Enums;
using CoiTelemetry.RealMod.Contracts.Ids;
using CoiTelemetry.RealMod.Mapping;
using Mafi;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Excavators;

namespace CoiTelemetry.RealMod.Collecting;

public sealed class VehicleMetrics
{
    private readonly EntityTracker _tracker;
    private readonly IProductFlowMetrics _productFlowMetrics;
    private readonly Vehicle _vehicle;
    
    private readonly EntityId _vehicleId;
    private int _observedTicks;
    private VehicleObservedState _observedState;
    private readonly Dictionary<VehicleObservedState, int> _stateCounters = new();

    private EntityId? _assignedTo;
    private int _deliveriesCompleted;
    private double _fuelConsumed;
    private double _distanceEmpty;
    private double _distanceLoaded;

    private long _totalDistance;
    private int _lastCargoAmount;
    private int _cargoDelta;
    private Duration _lastFuel;
    
    private readonly Dictionary<ProductId, double> _deliveredByProduct = new();
    private readonly Dictionary<ProductId, double> _producedByProduct = new();
    private readonly Dictionary<ProductId, double> _consumedByProduct = new();
    private readonly Dictionary<string, int> _jobsInfo = new();
    
    public VehicleMetrics(EntityTracker tracker, IProductFlowMetrics productFlowMetrics, Vehicle vehicle)
    {
        _tracker = tracker;
        _productFlowMetrics = productFlowMetrics;
        _vehicle = vehicle;
        _vehicleId = _tracker.Vehicle(vehicle);

        _totalDistance = vehicle.LifetimeDistanceTraveled.RawValue;
        _lastFuel = _vehicle.FuelTank.ValueOrNull?.RemainingDuration ?? Duration.Zero;
    }

    public void ObserveState()
    {
        _observedTicks++;
        _assignedTo = _tracker.Entity(_vehicle.AssignedTo.ValueOrNull);
        VehicleObservedState state;
        if (_vehicle is Excavator excavator)
        {
            state = GetExcavatorObservedState(excavator);
            if (state != _observedState && state == VehicleObservedState.Unloading)
            {
                var cargo = excavator.Cargo.GetEnumerator();
                while (cargo.MoveNext())
                {
                    var productId = _tracker.Product(cargo.Current.Key);
                    AddProduced(productId, cargo.Current.Value.Value);
                    _productFlowMetrics.AddMined(productId, cargo.Current.Value.Value);
                }
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
            state = GetVehicleState();
        }
        var fuelTank = _vehicle.FuelTank.ValueOrNull;
        if (fuelTank is not null)
        {
            var fuelTicks = fuelTank.RemainingDuration;
            var diff = _lastFuel - fuelTicks;
            var tankTicks = fuelTank.Proto.OneQuantityDuration.Ticks*fuelTank.Proto.Capacity.Value;
            var currentFuel = (double)diff.Ticks/tankTicks;
            AddFuelConsumed(currentFuel);
            _lastFuel = fuelTicks;
        }
        
        var currentJob = _vehicle.CurrentJob.ValueOrNull;

        if (currentJob is not null)
        {
            var msg = $"{currentJob.GetType()}({currentJob.JobInfo.Value})";
            _jobsInfo.TryGetValue(msg, out var count);
            _jobsInfo[msg] = count + 1;
        }
        var totalDistance = _vehicle.LifetimeDistanceTraveled.RawValue;
        AddMovedDistance(totalDistance-_totalDistance, _lastCargoAmount>0);
        
        _stateCounters.TryGetValue(state, out var ticks);
        _stateCounters[state] = ticks + 1;
        _totalDistance = totalDistance;
        _observedState = state;
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
    private VehicleObservedState GetPrimaryBlocker()
    {
        var best = _stateCounters.OrderByDescending(x => x.Value).First();
        return best.Value <= 0 ? VehicleObservedState.None : best.Key;
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
            AssignedTo: _assignedTo?.Value,
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
    }
    public void AddConsumed(ProductId productId, double amount)
    {
        if (amount <= 0)
        {
            return;
        }
        AddTo(_consumedByProduct, productId, amount);
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

    public void AddFuelConsumed(double fuelConsumed)
    {
        if (fuelConsumed <= 0)
        {
            return;
        }

        _fuelConsumed += fuelConsumed;
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
    private VehicleObservedState GetVehicleState()
    {

        if (_vehicle.NeedsJob)
        {
            return VehicleObservedState.Idle;
        }

        if (_vehicle.CannotWorkDueToLowFuel)
        {
            return VehicleObservedState.NoFuel;
        }

        if (_vehicle.IsStuck)
        {
            return VehicleObservedState.Stuck;
        }

        if (!_vehicle.Maintenance.CanWork())
        {
            return VehicleObservedState.Broke;
        }


        if (_vehicle.IsDriving)
        {
            return _lastCargoAmount>0 ? VehicleObservedState.MovingLoaded : VehicleObservedState.MovingEmpty;
        }

        if (_cargoDelta > 0)
        {
            return VehicleObservedState.Loading;
        }
        if (_cargoDelta < 0)
        {
            return VehicleObservedState.Unloading;
        }

        return VehicleObservedState.Waiting;
    }
    
    private static VehicleObservedState GetExcavatorObservedState(Excavator excavator)
    {
        switch (excavator.State)
        {
            case ExcavatorState.Idle:
                return VehicleObservedState.Idle;
            case ExcavatorState.DoJob:
                return VehicleObservedState.Working;
            case ExcavatorState.LoadTruck:
                return VehicleObservedState.Unloading;
            case ExcavatorState.WaitingForShovel:
                return VehicleObservedState.Loading;
            case ExcavatorState.WaitingForTruck:
                return VehicleObservedState.Waiting;
            case ExcavatorState.GettingUnstuck:
                return VehicleObservedState.Stuck;
            default:
                return VehicleObservedState.None;
        }
    }
}