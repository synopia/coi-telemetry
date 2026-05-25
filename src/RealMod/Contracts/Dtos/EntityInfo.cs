namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record EntityInfo(
    string Id,
    string? Type,
    string? Name
);