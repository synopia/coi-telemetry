using System;
using System.Collections.Generic;
using CoiTelemetry.RealMod.Collecting;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Enums;
using CoiTelemetry.RealMod.Runtime;
using Mafi;

namespace CoiTelemetry.RealMod.Aggregation;

public static class SummaryCombiner
{
    public static ExportSummary Combine(
        IReadOnlyList<ExportSummary> rows,
        string summaryId,
        SimStep step,
        bool includeNetworkAnalysis = true)
    {
        var totalTicks = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            totalTicks += rows[i].Meta.ObservedTicks;
        }

        var meta = new SummaryMeta(summaryId, totalTicks, step, DateTime.UtcNow);
        var machines = BuildMachineSummaries(rows);
        var vehicles = BuildVehicleSummaries(rows);
        var productFlow = BuildProductFlowSummaries(rows);
        var networkAnalysis = includeNetworkAnalysis
            ? BuildNetworkAnalysis(machines, productFlow)
            : (Graph: ProductDependencyGraph.Empty, Simulation: ProductDependencyImpactSimulation.Empty);

        return new ExportSummary(
            Meta: meta,
            Machines: machines,
            Vehicles: vehicles,
            ProductFlow: productFlow,
            DependencyGraph: networkAnalysis.Graph,
            ImpactSimulation: networkAnalysis.Simulation
        );
    }

    private static (ProductDependencyGraph Graph, ProductDependencyImpactSimulation Simulation) BuildNetworkAnalysis(
        IReadOnlyList<MachineSummaryRow> machines,
        IReadOnlyList<ProductFlowSummaryRow> productFlow)
    {
        ProductDependencyGraph graph;
        using (Profiler.Scope("BuildDependencyGraph"))
        {
            graph = ProductDependencyGraphBuilder.Build(machines, productFlow);
        }

        ProductDependencyImpactSimulation simulation;
        using (Profiler.Scope("BuildImpactSimulation"))
        {
            simulation = ProductImpactSimulator.Build(machines, productFlow);
        }

        return (graph, simulation);
    }

    private static IReadOnlyList<MachineSummaryRow> BuildMachineSummaries(IReadOnlyList<ExportSummary> rows)
    {
        var lookup = new Dictionary<string, MachineAggregate>(StringComparer.Ordinal);
        var aggregates = new List<MachineAggregate>();

        for (var i = 0; i < rows.Count; i++)
        {
            var summary = rows[i];
            var summaryTicks = summary.Meta.ObservedTicks;
            var machines = summary.Machines;

            for (var j = 0; j < machines.Count; j++)
            {
                var machine = machines[j];
                if (!lookup.TryGetValue(machine.MachineId, out var aggregate))
                {
                    aggregate = new MachineAggregate(machine.MachineId);
                    lookup.Add(machine.MachineId, aggregate);
                    aggregates.Add(aggregate);
                }

                aggregate.Add(machine, summaryTicks);
            }
        }

        var result = new MachineSummaryRow[aggregates.Count];
        for (var i = 0; i < aggregates.Count; i++)
        {
            result[i] = aggregates[i].Build();
        }

        return result;
    }

    private static IReadOnlyList<VehicleSummaryRow> BuildVehicleSummaries(IReadOnlyList<ExportSummary> rows)
    {
        var lookup = new Dictionary<string, VehicleAggregate>(StringComparer.Ordinal);
        var aggregates = new List<VehicleAggregate>();

        for (var i = 0; i < rows.Count; i++)
        {
            var summary = rows[i];
            var summaryTicks = summary.Meta.ObservedTicks;
            var vehicles = summary.Vehicles;

            for (var j = 0; j < vehicles.Count; j++)
            {
                var vehicle = vehicles[j];
                if (!lookup.TryGetValue(vehicle.VehicleId, out var aggregate))
                {
                    aggregate = new VehicleAggregate(vehicle.VehicleId);
                    lookup.Add(vehicle.VehicleId, aggregate);
                    aggregates.Add(aggregate);
                }

                aggregate.Add(vehicle, summaryTicks);
            }
        }

        var result = new VehicleSummaryRow[aggregates.Count];
        for (var i = 0; i < aggregates.Count; i++)
        {
            result[i] = aggregates[i].Build();
        }

        return result;
    }

    private static IReadOnlyList<ProductFlowSummaryRow> BuildProductFlowSummaries(IReadOnlyList<ExportSummary> rows)
    {
        var lookup = new Dictionary<string, ProductFlowAggregate>(StringComparer.Ordinal);
        var aggregates = new List<ProductFlowAggregate>();

        for (var i = 0; i < rows.Count; i++)
        {
            var summary = rows[i];
            var summaryTicks = summary.Meta.ObservedTicks;
            var products = summary.ProductFlow;

            for (var j = 0; j < products.Count; j++)
            {
                var product = products[j];
                if (!lookup.TryGetValue(product.ProductId, out var aggregate))
                {
                    aggregate = new ProductFlowAggregate(product.ProductId);
                    lookup.Add(product.ProductId, aggregate);
                    aggregates.Add(aggregate);
                }

                aggregate.Add(product, summaryTicks);
            }
        }

        var result = new ProductFlowSummaryRow[aggregates.Count];
        for (var i = 0; i < aggregates.Count; i++)
        {
            result[i] = aggregates[i].Build();
        }

        return result;
    }

    private static ObservedState GetPrimaryBlocker(Dictionary<ObservedState, int> stateCounters)
    {
        var bestState = ObservedState.Unknown;
        var bestTicks = 0;

        foreach (var entry in stateCounters)
        {
            if (entry.Key == ObservedState.Working || entry.Key == ObservedState.Idle)
            {
                continue;
            }

            if (entry.Value > bestTicks)
            {
                bestState = entry.Key;
                bestTicks = entry.Value;
            }
        }

        return bestTicks <= 0
            ? ObservedState.Unknown
            : bestState;
    }

    private static VehicleBlockerKind GetPrimaryVehicleBlocker(Dictionary<VehicleBlockerKind, int> blockerCounters)
    {
        var bestBlocker = VehicleBlockerKind.None;
        var bestTicks = 0;

        foreach (var entry in blockerCounters)
        {
            if (entry.Value > bestTicks)
            {
                bestBlocker = entry.Key;
                bestTicks = entry.Value;
            }
        }

        return bestTicks <= 0
            ? VehicleBlockerKind.None
            : bestBlocker;
    }

    private static MachinePotentialScenario[] ScalePotentialScenarios(
        IReadOnlyList<MachinePotentialScenario> scenarios,
        double totalSeconds)
    {
        if (scenarios.Count == 0)
        {
            return Array.Empty<MachinePotentialScenario>();
        }

        var result = new MachinePotentialScenario[scenarios.Count];
        for (var i = 0; i < scenarios.Count; i++)
        {
            var scenario = scenarios[i];
            result[i] = new MachinePotentialScenario(
                ScenarioId: scenario.ScenarioId,
                Label: scenario.Label,
                Factor: scenario.Factor,
                Inputs: ScalePotentialFlows(scenario.Inputs, totalSeconds),
                Outputs: ScalePotentialFlows(scenario.Outputs, totalSeconds));
        }

        return result;
    }

    private static ProductFlowSummary[] ScalePotentialFlows(IReadOnlyList<ProductFlowSummary> flows, double totalSeconds)
    {
        if (flows.Count == 0)
        {
            return Array.Empty<ProductFlowSummary>();
        }

        var result = new ProductFlowSummary[flows.Count];
        for (var i = 0; i < flows.Count; i++)
        {
            var flow = flows[i];
            result[i] = new ProductFlowSummary(
                ProductId: flow.ProductId,
                Amount: flow.PerMinute * totalSeconds / 60.0,
                PerMinute: flow.PerMinute);
        }

        return result;
    }

    private static ProductFlowSummary[] BuildFlowSummaries(Dictionary<string, double> amounts, double totalSeconds)
    {
        if (amounts.Count == 0)
        {
            return Array.Empty<ProductFlowSummary>();
        }

        var result = new ProductFlowSummary[amounts.Count];
        var index = 0;
        foreach (var entry in amounts)
        {
            result[index++] = new ProductFlowSummary(
                ProductId: entry.Key,
                Amount: entry.Value,
                PerMinute: MetricMath.PerMinute(entry.Value, totalSeconds));
        }

        Array.Sort(result, CompareProductFlows);
        return result;
    }

    private static int CompareProductFlows(ProductFlowSummary left, ProductFlowSummary right)
    {
        return StringComparer.Ordinal.Compare(left.ProductId, right.ProductId);
    }

    private static void AddFlowAmounts(Dictionary<string, double> target, IReadOnlyList<ProductFlowSummary> source)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var flow = source[i];
            if (target.TryGetValue(flow.ProductId, out var amount))
            {
                target[flow.ProductId] = amount + flow.Amount;
            }
            else
            {
                target.Add(flow.ProductId, flow.Amount);
            }
        }
    }

    private static void AddCounts<T>(Dictionary<T, int> target, IReadOnlyDictionary<T, int> source)
        where T : notnull
    {
        foreach (var entry in source)
        {
            if (target.TryGetValue(entry.Key, out var count))
            {
                target[entry.Key] = count + entry.Value;
            }
            else
            {
                target.Add(entry.Key, entry.Value);
            }
        }
    }

    private sealed class MachineAggregate
    {
        private readonly Dictionary<ObservedState, int> _uptimeTicks = new();
        private readonly Dictionary<string, double> _inputs = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _outputs = new(StringComparer.Ordinal);

        public MachineAggregate(string machineId)
        {
            MachineId = machineId;
        }

        public string MachineId { get; }
        public string? RecipeId { get; private set; }
        public int ObservedTicks { get; private set; }
        public double Maintenance { get; private set; }
        public double Power { get; private set; }
        public double Computing { get; private set; }
        public double Workers { get; private set; }
        public IReadOnlyList<ProductBufferSummary> InputBuffers { get; private set; } = Array.Empty<ProductBufferSummary>();
        public IReadOnlyList<ProductBufferSummary> OutputBuffers { get; private set; } = Array.Empty<ProductBufferSummary>();
        public IReadOnlyList<MachinePotentialScenario> PotentialScenarios { get; private set; } = Array.Empty<MachinePotentialScenario>();

        public void Add(MachineSummaryRow row, int summaryObservedTicks)
        {
            ObservedTicks += summaryObservedTicks;
            RecipeId = row.RecipeId;
            Maintenance = row.Maintenance;
            Power = row.Power;
            Computing = row.Computing;
            Workers = row.Workers;
            InputBuffers = row.InputBuffers;
            OutputBuffers = row.OutputBuffers;
            PotentialScenarios = row.PotentialScenarios;

            AddCounts(_uptimeTicks, row.UptimeTicks);
            AddFlowAmounts(_inputs, row.Inputs);
            AddFlowAmounts(_outputs, row.Outputs);
        }

        public MachineSummaryRow Build()
        {
            var totalSeconds = ObservedTicks * SimStep.SECONDS_PER_STEP;
            return new MachineSummaryRow(
                MachineId: MachineId,
                RecipeId: RecipeId,
                ObservedTicks: ObservedTicks,
                UptimePercent: MetricMath.Percent(_uptimeTicks, ObservedTicks),
                UptimeTicks: _uptimeTicks,
                Maintenance: Maintenance,
                Power: Power,
                Computing: Computing,
                Workers: Workers,
                Inputs: BuildFlowSummaries(_inputs, totalSeconds),
                Outputs: BuildFlowSummaries(_outputs, totalSeconds),
                InputBuffers: InputBuffers,
                OutputBuffers: OutputBuffers,
                PotentialScenarios: ScalePotentialScenarios(PotentialScenarios, totalSeconds),
                PrimaryBlocker: GetPrimaryBlocker(_uptimeTicks));
        }
    }

    private sealed class VehicleAggregate
    {
        private readonly Dictionary<ObservedState, int> _uptimeTicks = new();
        private readonly Dictionary<VehicleBlockerKind, int> _blockerTicks = new();
        private readonly Dictionary<string, double> _delivered = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _produced = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _consumed = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _jobs = new(StringComparer.Ordinal);

        public VehicleAggregate(string vehicleId)
        {
            VehicleId = vehicleId;
        }

        public string VehicleId { get; }
        public int ObservedTicks { get; private set; }
        public string? AssignedTo { get; private set; }
        public double Maintenance { get; private set; }
        public double Power { get; private set; }
        public double Computing { get; private set; }
        public double Workers { get; private set; }
        public double EmptyTravelDistance { get; private set; }
        public double LoadedTravelDistance { get; private set; }
        public int DeliveriesCompleted { get; private set; }
        public string? CurrentJob { get; private set; }
        public string? CurrentJobInfo { get; private set; }
        public string? CurrentGoal { get; private set; }
        public string PathFindingState { get; private set; } = string.Empty;
        public string DrivingState { get; private set; } = string.Empty;

        public void Add(VehicleSummaryRow row, int summaryObservedTicks)
        {
            ObservedTicks += summaryObservedTicks;
            AssignedTo = row.AssignedTo;
            Maintenance = row.Maintenance;
            Power = row.Power;
            Computing = row.Computing;
            Workers = row.Workers;
            EmptyTravelDistance += row.EmptyTravelDistance;
            LoadedTravelDistance += row.LoadedTravelDistance;
            DeliveriesCompleted += row.DeliveriesCompleted;
            CurrentJob = row.CurrentJob;
            CurrentJobInfo = row.CurrentJobInfo;
            CurrentGoal = row.CurrentGoal;
            PathFindingState = row.PathFindingState;
            DrivingState = row.DrivingState;

            AddCounts(_uptimeTicks, row.UptimeTicks);
            AddCounts(_blockerTicks, row.BlockerTicks);
            AddFlowAmounts(_delivered, row.Delivered);
            AddFlowAmounts(_produced, row.Produced);
            AddFlowAmounts(_consumed, row.Consumed);
            AddCounts(_jobs, row.Jobs);
        }

        public VehicleSummaryRow Build()
        {
            var totalSeconds = ObservedTicks * SimStep.SECONDS_PER_STEP;
            return new VehicleSummaryRow(
                VehicleId: VehicleId,
                AssignedTo: AssignedTo,
                ObservedTicks: ObservedTicks,
                UptimePercent: MetricMath.Percent(_uptimeTicks, ObservedTicks),
                UptimeTicks: _uptimeTicks,
                BlockerPercent: MetricMath.Percent(_blockerTicks, ObservedTicks),
                BlockerTicks: _blockerTicks,
                DeliveriesCompleted: DeliveriesCompleted,
                Delivered: BuildFlowSummaries(_delivered, totalSeconds),
                Maintenance: Maintenance,
                Power: Power,
                Computing: Computing,
                Workers: Workers,
                Produced: BuildFlowSummaries(_produced, totalSeconds),
                Consumed: BuildFlowSummaries(_consumed, totalSeconds),
                Jobs: _jobs,
                CurrentJob: CurrentJob,
                CurrentJobInfo: CurrentJobInfo,
                CurrentGoal: CurrentGoal,
                PathFindingState: PathFindingState,
                DrivingState: DrivingState,
                PrimaryDetailedBlocker: GetPrimaryVehicleBlocker(_blockerTicks),
                PrimaryBlocker: GetPrimaryBlocker(_uptimeTicks),
                EmptyTravelDistance: EmptyTravelDistance,
                LoadedTravelDistance: LoadedTravelDistance);
        }
    }

    private sealed class ProductFlowAggregate
    {
        private double _avgStoredTotal;
        private int _avgStoredCount;

        public ProductFlowAggregate(string productId)
        {
            ProductId = productId;
            MinStored = double.PositiveInfinity;
            MaxStored = double.NegativeInfinity;
        }

        public string ProductId { get; }
        public int ObservedTicks { get; private set; }
        public double LatestStored { get; private set; }
        public double LatestCapacity { get; private set; }
        public double LatestFillPercent { get; private set; }
        public double MinStored { get; private set; }
        public double MaxStored { get; private set; }
        public double ProducedAmount { get; private set; }
        public double ConsumedAmount { get; private set; }
        public double ImportedAmount { get; private set; }
        public double ExportedAmount { get; private set; }
        public double MinedAmount { get; private set; }
        public double DumpedAmount { get; private set; }
        public double LostAmount { get; private set; }

        public void Add(ProductFlowSummaryRow row, int summaryObservedTicks)
        {
            ObservedTicks += summaryObservedTicks;
            LatestStored = row.LatestStored;
            LatestCapacity = row.LatestCapacity;
            LatestFillPercent = row.LatestFillPercent;
            MinStored = Math.Min(MinStored, row.MinStored);
            MaxStored = Math.Max(MaxStored, row.MaxStored);
            _avgStoredTotal += row.AvgStored;
            _avgStoredCount++;

            ProducedAmount += row.ProducedAmount;
            ConsumedAmount += row.ConsumedAmount;
            ImportedAmount += row.ImportedAmount;
            ExportedAmount += row.ExportedAmount;
            MinedAmount += row.MinedAmount;
            DumpedAmount += row.DumpedAmount;
            LostAmount += row.LostAmount;
        }

        public ProductFlowSummaryRow Build()
        {
            var totalSeconds = ObservedTicks * SimStep.SECONDS_PER_STEP;
            var netAmount = ProducedAmount + MinedAmount + ImportedAmount - ConsumedAmount - ExportedAmount - DumpedAmount - LostAmount;
            var netPerMinute = MetricMath.PerMinute(netAmount, totalSeconds);
            var avgStored = _avgStoredCount <= 0 ? 0 : _avgStoredTotal / _avgStoredCount;
            var minStored = double.IsPositiveInfinity(MinStored) ? 0 : MinStored;
            var maxStored = double.IsNegativeInfinity(MaxStored) ? 0 : MaxStored;

            return new ProductFlowSummaryRow(
                ProductId: ProductId,
                ObservedTicks: ObservedTicks,
                LatestStored: LatestStored,
                LatestCapacity: LatestCapacity,
                LatestFillPercent: LatestFillPercent,
                MinStored: minStored,
                MaxStored: maxStored,
                AvgStored: avgStored,
                ProducedAmount: ProducedAmount,
                ConsumedAmount: ConsumedAmount,
                ImportedAmount: ImportedAmount,
                ExportedAmount: ExportedAmount,
                MinedAmount: MinedAmount,
                DumpedAmount: DumpedAmount,
                LostAmount: LostAmount,
                NetAmount: netAmount,
                ProducedPerMinute: MetricMath.PerMinute(ProducedAmount, totalSeconds),
                ConsumedPerMinute: MetricMath.PerMinute(ConsumedAmount, totalSeconds),
                NetPerMinute: netPerMinute,
                EstimatedMinutesUntilEmpty: MetricMath.EstimateMinutesUntilEmpty(LatestStored, netPerMinute),
                EstimatedMinutesUntilFull: MetricMath.EstimateMinutesUntilFull(LatestStored, LatestCapacity, netPerMinute));
        }
    }
}
