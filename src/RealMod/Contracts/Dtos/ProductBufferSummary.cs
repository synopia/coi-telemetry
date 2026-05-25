namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record ProductBufferSummary(
    string ProductId,
    double Stored,
    double Capacity,
    double FillPercent);