using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Dtos;

namespace CoiTelemetry.RealMod.Aggregation;

public static class ProductDependencyGraphBuilder
{
    private const string FullLocalCapacityScenarioId = "full-local-capacity";
    private const double Epsilon = 0.0001;

    public static ProductDependencyGraph Build(
        IReadOnlyList<MachineSummaryRow> machines,
        IReadOnlyList<ProductFlowSummaryRow> productFlow)
    {
        var products = productFlow.ToDictionary(
            product => product.ProductId,
            product => new MutableProductNode(product));

        var machineNodes = new List<ProductDependencyMachineNode>();
        var edges = new List<ProductDependencyEdge>();

        foreach (var machine in machines)
        {
            var fullScenario = machine.PotentialScenarios
                .FirstOrDefault(s => string.Equals(s.ScenarioId, FullLocalCapacityScenarioId, StringComparison.Ordinal));

            if (machine.RecipeId is null)
            {
                continue;
            }

            var currentInputs = IndexByProduct(machine.Inputs);
            var currentOutputs = IndexByProduct(machine.Outputs);
            var potentialInputs = IndexByProduct(fullScenario?.Inputs ?? machine.Inputs);
            var potentialOutputs = IndexByProduct(fullScenario?.Outputs ?? machine.Outputs);

            if (currentInputs.Count == 0 && currentOutputs.Count == 0 && potentialInputs.Count == 0 && potentialOutputs.Count == 0)
            {
                continue;
            }

            var currentInputPerMinute = currentInputs.Values.Sum(flow => flow.PerMinute);
            var potentialInputPerMinute = potentialInputs.Values.Sum(flow => flow.PerMinute);
            var currentOutputPerMinute = currentOutputs.Values.Sum(flow => flow.PerMinute);
            var potentialOutputPerMinute = potentialOutputs.Values.Sum(flow => flow.PerMinute);
            var currentUtilizationFactor = potentialOutputPerMinute <= Epsilon
                ? 1
                : Clamp01(currentOutputPerMinute / potentialOutputPerMinute);

            machineNodes.Add(new ProductDependencyMachineNode(
                MachineId: machine.MachineId,
                RecipeId: machine.RecipeId,
                CurrentInputPerMinute: currentInputPerMinute,
                PotentialInputPerMinute: potentialInputPerMinute,
                InputHeadroomPerMinute: PositiveDelta(currentInputPerMinute, potentialInputPerMinute),
                CurrentOutputPerMinute: currentOutputPerMinute,
                PotentialOutputPerMinute: potentialOutputPerMinute,
                OutputHeadroomPerMinute: PositiveDelta(currentOutputPerMinute, potentialOutputPerMinute),
                CurrentUtilizationFactor: currentUtilizationFactor,
                PrimaryBlocker: machine.PrimaryBlocker));

            foreach (var productId in currentInputs.Keys.Union(potentialInputs.Keys).OrderBy(id => id))
            {
                var current = currentInputs.TryGetValue(productId, out var currentFlow) ? currentFlow.PerMinute : 0;
                var potential = potentialInputs.TryGetValue(productId, out var potentialFlow) ? potentialFlow.PerMinute : current;
                var product = GetOrCreateProduct(products, productId);

                product.CurrentDownstreamDemandPerMinute += current;
                product.PotentialDownstreamDemandPerMinute += potential;
                if (PositiveDelta(current, potential) > Epsilon)
                {
                    product.ConsumerMachineIds.Add(machine.MachineId);
                }

                edges.Add(new ProductDependencyEdge(
                    SourceNodeId: productId,
                    TargetNodeId: machine.MachineId,
                    ProductId: productId,
                    CurrentPerMinute: current,
                    PotentialPerMinute: potential,
                    HeadroomPerMinute: PositiveDelta(current, potential)));
            }

            foreach (var productId in currentOutputs.Keys.Union(potentialOutputs.Keys).OrderBy(id => id))
            {
                var current = currentOutputs.TryGetValue(productId, out var currentFlow) ? currentFlow.PerMinute : 0;
                var potential = potentialOutputs.TryGetValue(productId, out var potentialFlow) ? potentialFlow.PerMinute : current;
                var product = GetOrCreateProduct(products, productId);

                product.CurrentLocalProducedPerMinute += current;
                product.PotentialLocalProducedPerMinute += potential;
                if (PositiveDelta(current, potential) > Epsilon)
                {
                    product.ProducerMachineIds.Add(machine.MachineId);
                }

                edges.Add(new ProductDependencyEdge(
                    SourceNodeId: machine.MachineId,
                    TargetNodeId: productId,
                    ProductId: productId,
                    CurrentPerMinute: current,
                    PotentialPerMinute: potential,
                    HeadroomPerMinute: PositiveDelta(current, potential)));
            }
        }

        var productNodes = products.Values
            .Select(product => product.BuildNode())
            .OrderBy(product => product.ProductId)
            .ToArray();

        var opportunities = products.Values
            .Select(product => product.BuildOpportunity())
            .Where(opportunity =>
                opportunity.LocalProductionHeadroomPerMinute > Epsilon ||
                opportunity.DownstreamDemandHeadroomPerMinute > Epsilon)
            .OrderByDescending(opportunity =>
                Math.Max(opportunity.LocalProductionHeadroomPerMinute, opportunity.DownstreamDemandHeadroomPerMinute))
            .ThenBy(opportunity => opportunity.ProductId)
            .ToArray();

        return new ProductDependencyGraph(
            Products: productNodes,
            Machines: machineNodes.OrderBy(machine => machine.MachineId).ToArray(),
            Edges: edges
                .OrderBy(edge => edge.SourceNodeId)
                .ThenBy(edge => edge.TargetNodeId)
                .ThenBy(edge => edge.ProductId)
                .ToArray(),
            Opportunities: opportunities);
    }

    private static MutableProductNode GetOrCreateProduct(
        IDictionary<string, MutableProductNode> products,
        string productId)
    {
        if (products.TryGetValue(productId, out var product))
        {
            return product;
        }

        product = new MutableProductNode(productId);
        products[productId] = product;
        return product;
    }

    private static Dictionary<string, ProductFlowSummary> IndexByProduct(IReadOnlyList<ProductFlowSummary> flows)
    {
        return flows.ToDictionary(flow => flow.ProductId, flow => flow);
    }

    private static double PositiveDelta(double current, double potential)
    {
        return Math.Max(0, potential - current);
    }

    private static double Clamp01(double value)
    {
        return Math.Max(0, Math.Min(1, value));
    }

    private sealed class MutableProductNode
    {
        public MutableProductNode(ProductFlowSummaryRow productFlow)
            : this(productFlow.ProductId)
        {
            ProducedPerMinute = productFlow.ProducedPerMinute;
            ConsumedPerMinute = productFlow.ConsumedPerMinute;
            NetPerMinute = productFlow.NetPerMinute;
            Stored = productFlow.LatestStored;
            Capacity = productFlow.LatestCapacity;
            FillPercent = productFlow.LatestFillPercent;
        }

        public MutableProductNode(string productId)
        {
            ProductId = productId;
        }

        public string ProductId { get; }
        public double ProducedPerMinute { get; }
        public double ConsumedPerMinute { get; }
        public double NetPerMinute { get; }
        public double Stored { get; }
        public double Capacity { get; }
        public double FillPercent { get; }
        public double CurrentLocalProducedPerMinute { get; set; }
        public double PotentialLocalProducedPerMinute { get; set; }
        public double CurrentDownstreamDemandPerMinute { get; set; }
        public double PotentialDownstreamDemandPerMinute { get; set; }
        public HashSet<string> ProducerMachineIds { get; } = new();
        public HashSet<string> ConsumerMachineIds { get; } = new();

        public ProductDependencyProductNode BuildNode()
        {
            return new ProductDependencyProductNode(
                ProductId: ProductId,
                ProducedPerMinute: ProducedPerMinute,
                ConsumedPerMinute: ConsumedPerMinute,
                NetPerMinute: NetPerMinute,
                Stored: Stored,
                Capacity: Capacity,
                FillPercent: FillPercent,
                CurrentLocalProducedPerMinute: CurrentLocalProducedPerMinute,
                PotentialLocalProducedPerMinute: PotentialLocalProducedPerMinute,
                LocalProductionHeadroomPerMinute: PositiveDelta(CurrentLocalProducedPerMinute, PotentialLocalProducedPerMinute),
                CurrentDownstreamDemandPerMinute: CurrentDownstreamDemandPerMinute,
                PotentialDownstreamDemandPerMinute: PotentialDownstreamDemandPerMinute,
                DownstreamDemandHeadroomPerMinute: PositiveDelta(CurrentDownstreamDemandPerMinute, PotentialDownstreamDemandPerMinute));
        }

        public ProductDependencyOpportunity BuildOpportunity()
        {
            var localProductionHeadroom = PositiveDelta(CurrentLocalProducedPerMinute, PotentialLocalProducedPerMinute);
            var downstreamDemandHeadroom = PositiveDelta(CurrentDownstreamDemandPerMinute, PotentialDownstreamDemandPerMinute);
            return new ProductDependencyOpportunity(
                ProductId: ProductId,
                CurrentLocalProducedPerMinute: CurrentLocalProducedPerMinute,
                PotentialLocalProducedPerMinute: PotentialLocalProducedPerMinute,
                LocalProductionHeadroomPerMinute: localProductionHeadroom,
                CurrentDownstreamDemandPerMinute: CurrentDownstreamDemandPerMinute,
                PotentialDownstreamDemandPerMinute: PotentialDownstreamDemandPerMinute,
                DownstreamDemandHeadroomPerMinute: downstreamDemandHeadroom,
                NetHeadroomPerMinute: localProductionHeadroom - downstreamDemandHeadroom,
                ProducerMachineIds: ProducerMachineIds.OrderBy(id => id).ToArray(),
                ConsumerMachineIds: ConsumerMachineIds.OrderBy(id => id).ToArray());
        }
    }
}
