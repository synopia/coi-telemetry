namespace CoiTelemetry.RealMod.Contracts.Dtos;


public sealed record ProductFlowSummary(
    string ProductId,
    double Amount,
    double PerMinute
);
