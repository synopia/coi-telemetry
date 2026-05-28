using System;
using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record ProductDependencyImpactSimulation(
    IReadOnlyList<ImpactMachineSimulationRow> Machines,
    IReadOnlyList<ImpactProductSimulationRow> Products,
    IReadOnlyList<ImpactConstraintRow> Constraints
)
{
    public static ProductDependencyImpactSimulation Empty { get; } = new(
        Machines: Array.Empty<ImpactMachineSimulationRow>(),
        Products: Array.Empty<ImpactProductSimulationRow>(),
        Constraints: Array.Empty<ImpactConstraintRow>());
}

public sealed record ImpactMachineSimulationRow(
    string MachineId,
    string? RecipeId,
    double RealizedHeadroomFactor,
    double CurrentInputPerMinute,
    double PotentialInputPerMinute,
    double SimulatedInputPerMinute,
    double CurrentOutputPerMinute,
    double PotentialOutputPerMinute,
    double SimulatedOutputPerMinute,
    IReadOnlyList<string> LimitingProducts,
    [property:JsonConverter(typeof(StringEnumConverter))]
    ObservedState PrimaryBlocker
);

public sealed record ImpactProductSimulationRow(
    string ProductId,
    double CurrentNetPerMinute,
    double BaselineSurplusPerMinute,
    double AdditionalProducedPerMinute,
    double AdditionalConsumedPerMinute,
    double SimulatedNetPerMinute,
    double ResidualSurplusPerMinute,
    bool IsLimiting
);

public sealed record ImpactConstraintRow(
    string ProductId,
    double BaselineSurplusPerMinute,
    double AdditionalSupplyPerMinute,
    double RequestedAdditionalDemandPerMinute,
    double FeasibleAdditionalDemandPerMinute,
    double SatisfactionPercent
);
