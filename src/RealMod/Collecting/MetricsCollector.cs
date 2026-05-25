using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Ids;
using CoiTelemetry.RealMod.Mapping;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Simulation;

namespace CoiTelemetry.RealMod.Collecting;

public sealed class MetricsCollector:IProductFlowMetrics
{
    private readonly EntityTracker _tracker;
    private readonly IEntitiesManager _entitiesManager;
    private readonly ISimLoopEvents _events;

    private readonly Dictionary<EntityId, MachineMetrics> _machines = new();
    private readonly Dictionary<EntityId, VehicleMetrics> _vehicles = new();
    private readonly Dictionary<ProductId, ProductFlowMetrics> _products = new();
    
    private int _windowObservedTicks;
    
    public MetricsCollector(IEntitiesManager entitiesManager, ISimLoopEvents events)
    {
        _entitiesManager = entitiesManager;
        _events = events;
        _tracker = new EntityTracker(new ExportIdMapper());
    }
    
    public void ObserveSimulationTick()
    {
        _windowObservedTicks++;

        ObserveMachines();
        ObserveVehicles();
    }

    private void ObserveMachines()
    {
        foreach (var machine in _entitiesManager.GetAllEntitiesOfType<Machine>())
        {
            var metrics = GetMachineMetrics(machine);
            metrics.ObserveState();
        }
    }

    private void ObserveVehicles()
    {
        foreach (var vehicle in _entitiesManager.GetAllEntitiesOfType<Vehicle>())
        {
            var metrics = GetVehicleMetrics(vehicle);
            metrics.ObserveState();
        }
    }

    public ExportSummary BuildSummary()
    {
        var meta = new SummaryMeta($"tick_{_events.CurrentStep.Value}", _windowObservedTicks, _events.CurrentStep, DateTime.UtcNow);
        return new ExportSummary(
            Meta: meta,
            Machines: BuildMachineSummaries().ToList(),
            Vehicles: BuildVehicleSummaries().ToList(),
            ProductFlow: BuildProductFlowSummaries().ToList()
            );
    }

    private IEnumerable<MachineSummaryRow> BuildMachineSummaries()
    {
        foreach (var metric in _machines.Values)
        {
            yield return metric.BuildSummaryRow();
        }
    }

    private IEnumerable<VehicleSummaryRow> BuildVehicleSummaries()
    {
        foreach (var metric in _vehicles.Values)
        {
            yield return metric.BuildSummaryRow();
        }
    }

    private IEnumerable<ProductFlowSummaryRow> BuildProductFlowSummaries()
    {
        var storage = BuildCurrentProductStorageIndex();
        foreach (var metric in _products.Values)
        {
            storage.TryGetValue(metric.ProductId, out var current);
            yield return metric.BuildSummaryRow(current, _windowObservedTicks);
        }
    }

    private Dictionary<ProductId, ProductStorage> BuildCurrentProductStorageIndex()
    {
        var result = new Dictionary<ProductId, ProductStorage>();

        void Add(ProductId productId, double stored, double capacity)
        {
            result.TryGetValue(productId, out var current);
            result[productId] = new ProductStorage(current.Stored+stored, current.Capacity+capacity);
        }

        foreach (var storage in _entitiesManager.GetAllEntitiesOfType<Storage>())
        {
            var product = storage.StoredProduct.ValueOrNull;
            if (product is not null)
            {
                Add(_tracker.Product(product), storage.CurrentQuantity.Value, storage.UsableCapacity.Value+storage.CurrentQuantity.Value);
            }
        }
        
        foreach (var machine in _entitiesManager.GetAllEntitiesOfType<Machine>())
        {
            if (machine is { LastRecipeInProgress: { ValueOrNull: { } recipe} })
            {
                recipe.AllInputs.ForEach(input => Add(_tracker.Product(input.Product),machine.GetInputQuantityFor(input.Product).Value, machine.GetOutputCapacityFor(input.Product).Value));
                recipe.AllOutputs.ForEach(output => Add(_tracker.Product(output.Product),machine.GetOutputQuantityFor(output.Product).Value,machine.GetOutputCapacityFor(output.Product).Value));
            }
        }
        return result;
    }

    public void ResetWindowCounters()
    {
        _windowObservedTicks = 0;
        foreach (var machine in _machines.Values)
        {
            machine.ResetWindow();
        }

        foreach (var vehicle in _vehicles.Values)
        {
            vehicle.ResetWindow();
        }

        foreach (var product in _products.Values)
        {
            product.ResetWindow();
        }
    }
    
    
    private MachineMetrics GetMachineMetrics(Machine machine)
    {
        var id = _tracker.Machine(machine);
        if (!_machines.TryGetValue(id, out var metrics))
        {
            metrics = new MachineMetrics(_tracker, this, machine);
            _machines.Add(id, metrics);
        }

        return metrics;
    }

    private VehicleMetrics GetVehicleMetrics(Vehicle vehicle)
    {
        var id = _tracker.Vehicle(vehicle);
        if (!_vehicles.TryGetValue(id, out var metrics))
        {
            metrics = new VehicleMetrics(_tracker, this, vehicle);
            _vehicles.Add(id, metrics);
        }

        return metrics;
    }

    private ProductFlowMetrics GetProductFlow(ProductId id)
    {
        if (!_products.TryGetValue(id, out var metrics))
        {
            metrics = new ProductFlowMetrics(id);
            _products.Add(id, metrics);
        }

        return metrics;
    }

    public void AddProduced(ProductId productId, double amount)
    {
        GetProductFlow(productId).AddProduced(amount);
    }

    public void AddConsumed(ProductId productId, double amount)
    {
        GetProductFlow(productId).AddConsumed(amount);
    }

    public void AddImported(ProductId productId, double amount)
    {
        GetProductFlow(productId).AddImported(amount);
    }

    public void AddExported(ProductId productId, double amount)
    {
        GetProductFlow(productId).AddExported(amount);
    }

    public void AddMined(ProductId productId, double amount)
    {
        GetProductFlow(productId).AddMined(amount);
    }

    public void AddDumped(ProductId productId, double amount)
    {
        GetProductFlow(productId).AddDumped(amount);
    }

    public void AddLost(ProductId productId, double amount)
    {
        GetProductFlow(productId).AddLost(amount);
    }
}