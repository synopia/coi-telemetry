using System.Collections.Generic;

namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record MachinePotentialScenario(
    string ScenarioId,
    string Label,
    double Factor,
    IReadOnlyList<ProductFlowSummary> Inputs,
    IReadOnlyList<ProductFlowSummary> Outputs
);
