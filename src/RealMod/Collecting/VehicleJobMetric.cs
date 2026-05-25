using System;
using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Enums;
using CoiTelemetry.RealMod.Mapping;
using Mafi;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Vehicles.Jobs;
using Mafi.Core.Vehicles.Trucks;

namespace CoiTelemetry.RealMod.Collecting;

/*
public enum VehicleJobType
{
    AttachRocketToLaunchPad,
    CargoDelivery,
    CargoPickup,
    ChainedNavigation,
    CleanExcavator,
    DockAtDock,
    DriveTo,
    Dumping,
    Empty,
    GetUnstuck,
    Mining,
    MixedCargoDelivery,
    NavigateTo,
    RecoverVehicle,
    RefuelOtherVehicle,
    RefuelSelf,
    ReturnHome,
    RocketAssemblyAttach,
    ScrapOrReplace,
    ShipNavigateTo,
    ShipUndock,
    Spawn,
    SurfaceModification,
    TreeHarvesterLoadTruck,
    TreeHarvesting,
    TreePlanting,
    VehicleQueue,
    Waiting
}
*/

public readonly record struct VehicleJobTracker(
    Action<IVehicleJob>? OnJobStart=null,
    Action<IVehicleJob>? OnJobChange=null,
    Action<IVehicleJob>? OnJobDone=null);

public interface IVehicleJobMetric
{
    VehicleObservedState? Process(Vehicle vehicle);
}
public class TruckJobMetric : IVehicleJobMetric
{
    private readonly EntityTracker _tracker;

    private readonly Dictionary<Type, VehicleJobTracker> _jobTrackers = new();
    private readonly Truck _truck;
    private readonly VehicleMetrics _metrics;
    private readonly IProductFlowMetrics _productFlowMetrics;
    private VehicleObservedState? _returnedState;
    private IVehicleJob? _currentJob;
    private string? _currentJobInfo;

    
    public TruckJobMetric(EntityTracker tracker, Truck truck, VehicleMetrics metrics, IProductFlowMetrics productFlowMetrics)
    {
        _tracker = tracker;
        _truck = truck;
        _metrics = metrics;
        _productFlowMetrics = productFlowMetrics;
        _jobTrackers[typeof(CargoDeliveryJob)] = new VehicleJobTracker(
            OnJobStart:j=>
            {
                _metrics.AddDeliveryCompleted(
                    _tracker.Product(((CargoDeliveryJob)j).CargoToDeliver.Product),
                    ((CargoDeliveryJob)j).CargoToDeliver.Quantity.Value);
                _returnedState = VehicleObservedState.Unloading;
            });
        _jobTrackers[typeof(CargoPickUpJob)] = new VehicleJobTracker(
            OnJobStart: j => _returnedState = VehicleObservedState.Loading);
        _jobTrackers[typeof(ChainedNavigationJob)] = new VehicleJobTracker(
            OnJobStart: j => _returnedState = VehicleObservedState.Waiting);
        _jobTrackers[typeof(DumpingJob)] = new VehicleJobTracker(
            OnJobStart: j =>
            {
                var cargo = _truck.Cargo.GetEnumerator();
                while (cargo.MoveNext())
                {
                    var productId = _tracker.Product(cargo.Current.Key);
                    _metrics.AddConsumed(productId, cargo.Current.Value.Value);
                    _productFlowMetrics.AddDumped(productId, cargo.Current.Value.Value);
                }
            }
            );
        _jobTrackers[typeof(MixedCargoDeliveryJob)] = new VehicleJobTracker(
            OnJobStart:j=>
            {
                var cargo = _truck.Cargo.GetEnumerator();
                while (cargo.MoveNext())
                {
                    var productId = _tracker.Product(cargo.Current.Key);
                    _metrics.AddDeliveryCompleted(productId, cargo.Current.Value.Value);
                    // _productFlowMetrics.AddDumped(productId, cargo.Current.Value.Value);
                }

                _returnedState = VehicleObservedState.Unloading;
            });
        _jobTrackers[typeof(RefuelOtherVehicleJob)] = new VehicleJobTracker(
            OnJobStart: j => _returnedState = VehicleObservedState.Working);
        _jobTrackers[typeof(VehicleQueueJob<Vehicle>)] = new VehicleJobTracker(
            OnJobChange: j =>_returnedState=j.JobInfo.Value.StartsWith("Driving") ? VehicleObservedState.Working : VehicleObservedState.Waiting
            );
        _jobTrackers[typeof(VehicleQueueJob<Truck>)] = new VehicleJobTracker(
            OnJobChange: j =>_returnedState=j.JobInfo.Value.StartsWith("Driving") ? VehicleObservedState.Working : VehicleObservedState.Waiting
            );
    }
    
    public VehicleObservedState? Process(Vehicle vehicle)
    {
        var newJob = (IVehicleJob)vehicle.CurrentJob.ValueOrNull;
        if (newJob?.Id != _currentJob?.Id)
        {
            if (_currentJob is not null && _jobTrackers.TryGetValue(_currentJob.GetType(), out var tracker) && tracker.OnJobDone != null)
            {
                tracker.OnJobDone(_currentJob);
            }

            _returnedState = null;
            if(newJob is not null && _jobTrackers.TryGetValue(newJob.GetType(), out var newTracker))
            {
                if(newTracker.OnJobStart is not null)
                {
                    newTracker.OnJobStart(newJob);
                }

                if (newTracker.OnJobChange is not null)
                {
                    newTracker.OnJobChange(newJob);
                }
            }
        }
        else if( _currentJob is not null)
        {
            var newJobInfo = _currentJob?.JobInfo.Value;
            if (newJobInfo != _currentJobInfo)
            {
                _currentJobInfo = newJobInfo;
                if (_jobTrackers.TryGetValue(_currentJob!.GetType(), out var tracker) && tracker.OnJobChange != null)
                {
                    tracker.OnJobChange(_currentJob);
                }
            }
        }
        _currentJob = newJob;
        return _returnedState;
    }
}