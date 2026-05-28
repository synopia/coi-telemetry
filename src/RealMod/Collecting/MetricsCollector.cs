using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Ids;
using CoiTelemetry.RealMod.Mapping;
using CoiTelemetry.RealMod.Runtime;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Products;
using Mafi.Core.Simulation;

namespace CoiTelemetry.RealMod.Collecting;

public sealed class MetricsCollector : IProductFlowMetrics, IDisposable
{
    private readonly IModContext _context;
    private readonly EntityTracker _tracker;
    private readonly IEntitiesManager _entitiesManager;
    private readonly ISimLoopEvents _events;
    private readonly IProductsManager _productsManager;

    private readonly Dictionary<Mafi.Core.EntityId, MachineMetrics> _machines = new();
    private readonly Dictionary<Mafi.Core.EntityId, VehicleMetrics> _vehicles = new();
    private readonly Dictionary<ProductId, ProductFlowMetrics> _products = new();
    private MachineMetrics[] _machineSnapshot = Array.Empty<MachineMetrics>();
    private VehicleMetrics[] _vehicleSnapshot = Array.Empty<VehicleMetrics>();
    private bool _machineSnapshotDirty = true;
    private bool _vehicleSnapshotDirty = true;
    private int _windowObservedTicks;
    
    public MetricsCollector(IModContext context, IEntitiesManager entitiesManager, ISimLoopEvents events)
    {
        _context = context;
        _entitiesManager = entitiesManager;
        _events = events;
        _productsManager = context.Resolver.Resolve<IProductsManager>();
        _tracker = new EntityTracker(new IdTracker());

        InitializeTrackedEntities();
        _entitiesManager.EntityAdded.Add<MetricsCollector>(this, OnEntityAdded);
        _entitiesManager.EntityRemoved.Add<MetricsCollector>(this, OnEntityRemoved);
    }
    
    public void ObserveSimulationTick()
    {
        _windowObservedTicks++;

        using (Profiler.Scope("ObserveMachines"))
        {
            ObserveMachines();
        }

        using (Profiler.Scope("ObserveVehicles"))
        {
            ObserveVehicles();
        }
    }

    private void ObserveMachines()
    {
        RefreshSnapshotsIfNeeded();
        var currentStep = _events.CurrentStep;
        foreach (var metrics in _machineSnapshot)
        {
            if (metrics.ShouldUpdate(currentStep))
            {
                metrics.Update(currentStep);
            }
        }
    }

    private void ObserveVehicles()
    {
        RefreshSnapshotsIfNeeded();
        var currentStep = _events.CurrentStep;
        foreach (var metrics in _vehicleSnapshot)
        {
            if (metrics.ShouldUpdate(currentStep))
            {
                metrics.Update(currentStep);
            }
        }
    }

    public ExportSummary BuildSummary(bool includeNetworkAnalysis = true)
    {
        using (Profiler.Scope("FlushPendingObservations"))
        {
            FlushPendingObservations();
        }

        var meta = new SummaryMeta($"tick_{_events.CurrentStep.Value}", _windowObservedTicks, _events.CurrentStep, DateTime.UtcNow);
        MachineSummaryRow[] machineSummaries;
        using (Profiler.Scope("BuildMachineSummaries"))
        {
            machineSummaries = BuildMachineSummaries().ToArray();
        }

        VehicleSummaryRow[] vehicleSummaries;
        using (Profiler.Scope("BuildVehicleSummaries"))
        {
            vehicleSummaries = BuildVehicleSummaries().ToArray();
        }
        ProductFlowSummaryRow[] productSummaries;
        using (Profiler.Scope("BuildProductFlowSummaries"))
        {
            productSummaries = BuildProductFlowSummaries().ToArray();
        }

        var dependencyGraph = ProductDependencyGraph.Empty;
        var impactSimulation = ProductDependencyImpactSimulation.Empty;
        if (includeNetworkAnalysis)
        {
            using (Profiler.Scope("BuildDependencyGraph"))
            {
                dependencyGraph = Aggregation.ProductDependencyGraphBuilder.Build(machineSummaries, productSummaries);
            }

            using (Profiler.Scope("BuildImpactSimulation"))
            {
                impactSimulation = Aggregation.ProductImpactSimulator.Build(machineSummaries, productSummaries);
            }
        }

        return new ExportSummary(
            Meta: meta,
            Machines: machineSummaries,
            Vehicles: vehicleSummaries,
            ProductFlow: productSummaries,
            DependencyGraph: dependencyGraph,
            ImpactSimulation: impactSimulation
            );
    }

    public IReadOnlyList<MetaInfo> BuildMetadata()
    {
        return _tracker.Meta
            .OrderBy(meta => meta.Id)
            .ToArray();
    }

    private IEnumerable<MachineSummaryRow> BuildMachineSummaries()
    {
        RefreshSnapshotsIfNeeded();
        foreach (var metric in _machineSnapshot)
        {
            yield return metric.BuildSummaryRow();
        }
    }

    private IEnumerable<VehicleSummaryRow> BuildVehicleSummaries()
    {
        RefreshSnapshotsIfNeeded();
        foreach (var metric in _vehicleSnapshot)
        {
            yield return metric.BuildSummaryRow();
        }
    }

    private IEnumerable<ProductFlowSummaryRow> BuildProductFlowSummaries()
    {
        foreach (var metric in _products.Values)
        {
            yield return metric.BuildSummaryRow(BuildCurrentProductStorage(metric.ProductId), _windowObservedTicks);
        }
    }

    private ProductStorage BuildCurrentProductStorage(ProductId productId)
    {
        if (!_tracker.TryGetProduct(productId, out var product))
        {
            return default;
        }

        var stats = _productsManager.GetStatsFor(product);
        return new ProductStorage(
            Stored: (double)stats.StoredQuantityTotal.Value,
            Capacity: (double)stats.StorageCapacity.Value);
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
    
    
    private void InitializeTrackedEntities()
    {
        foreach (var machine in _entitiesManager.GetAllEntitiesOfType<Machine>())
        {
            RegisterMachine(machine);
        }

        foreach (var vehicle in _entitiesManager.GetAllEntitiesOfType<Vehicle>())
        {
            RegisterVehicle(vehicle);
        }
    }

    private void OnEntityAdded(IEntity entity)
    {
        if (entity is Machine machine)
        {
            RegisterMachine(machine);
            return;
        }

        if (entity is Vehicle vehicle)
        {
            RegisterVehicle(vehicle);
        }
    }

    private void OnEntityRemoved(IEntity entity)
    {
        if (_machines.Remove(entity.Id))
        {
            _machineSnapshotDirty = true;
        }

        if (_vehicles.Remove(entity.Id))
        {
            _vehicleSnapshotDirty = true;
        }
    }

    private void RegisterMachine(Machine machine)
    {
        if (_machines.ContainsKey(machine.Id))
        {
            return;
        }

        _machines[machine.Id] = new MachineMetrics(_context, _tracker, this, machine);
        _machineSnapshotDirty = true;
    }

    private void RegisterVehicle(Vehicle vehicle)
    {
        if (_vehicles.ContainsKey(vehicle.Id))
        {
            return;
        }

        _vehicles[vehicle.Id] = VehicleMetrics.Create(_context, _tracker, this, vehicle);
        _vehicleSnapshotDirty = true;
    }

    private void RefreshSnapshotsIfNeeded()
    {
        if (_machineSnapshotDirty)
        {
            _machineSnapshot = _machines.Values.ToArray();
            _machineSnapshotDirty = false;
        }

        if (_vehicleSnapshotDirty)
        {
            _vehicleSnapshot = _vehicles.Values.ToArray();
            _vehicleSnapshotDirty = false;
        }
    }

    private void FlushPendingObservations()
    {
        RefreshSnapshotsIfNeeded();
        var currentStep = _events.CurrentStep;
        foreach (var metrics in _machineSnapshot)
        {
            metrics.Flush(currentStep);
        }

        foreach (var metrics in _vehicleSnapshot)
        {
            metrics.Flush(currentStep);
        }
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

    public void Dispose()
    {
        _entitiesManager.EntityAdded.Remove<MetricsCollector>(this, OnEntityAdded);
        _entitiesManager.EntityRemoved.Remove<MetricsCollector>(this, OnEntityRemoved);
    }
}
