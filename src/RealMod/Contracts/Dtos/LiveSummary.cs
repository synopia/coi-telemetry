namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record LiveSummary(
    ExportSummary Window10s,
    ExportSummary Window1m,
    ExportSummary Window5m,
    ExportSummary Window10m
    );