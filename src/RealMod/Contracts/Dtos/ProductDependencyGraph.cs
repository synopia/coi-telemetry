using System;
using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record ProductDependencyGraph(
    IReadOnlyList<ProductDependencyProductNode> Products,
    IReadOnlyList<ProductDependencyMachineNode> Machines,
    IReadOnlyList<ProductDependencyEdge> Edges,
    IReadOnlyList<ProductDependencyOpportunity> Opportunities
)
{
    public static ProductDependencyGraph Empty { get; } = new(
        Products: Array.Empty<ProductDependencyProductNode>(),
        Machines: Array.Empty<ProductDependencyMachineNode>(),
        Edges: Array.Empty<ProductDependencyEdge>(),
        Opportunities: Array.Empty<ProductDependencyOpportunity>());
}

public sealed record ProductDependencyProductNode(
    string ProductId,
    double ProducedPerMinute,
    double ConsumedPerMinute,
    double NetPerMinute,
    double Stored,
    double Capacity,
    double FillPercent,
    double CurrentLocalProducedPerMinute,
    double PotentialLocalProducedPerMinute,
    double LocalProductionHeadroomPerMinute,
    double CurrentDownstreamDemandPerMinute,
    double PotentialDownstreamDemandPerMinute,
    double DownstreamDemandHeadroomPerMinute
);

public sealed record ProductDependencyMachineNode(
    string MachineId,
    string? RecipeId,
    double CurrentInputPerMinute,
    double PotentialInputPerMinute,
    double InputHeadroomPerMinute,
    double CurrentOutputPerMinute,
    double PotentialOutputPerMinute,
    double OutputHeadroomPerMinute,
    double CurrentUtilizationFactor,
    [property:JsonConverter(typeof(StringEnumConverter))]
    ObservedState PrimaryBlocker
);

public sealed record ProductDependencyEdge(
    string SourceNodeId,
    string TargetNodeId,
    string ProductId,
    double CurrentPerMinute,
    double PotentialPerMinute,
    double HeadroomPerMinute
);

public sealed record ProductDependencyOpportunity(
    string ProductId,
    double CurrentLocalProducedPerMinute,
    double PotentialLocalProducedPerMinute,
    double LocalProductionHeadroomPerMinute,
    double CurrentDownstreamDemandPerMinute,
    double PotentialDownstreamDemandPerMinute,
    double DownstreamDemandHeadroomPerMinute,
    double NetHeadroomPerMinute,
    IReadOnlyList<string> ProducerMachineIds,
    IReadOnlyList<string> ConsumerMachineIds
);
