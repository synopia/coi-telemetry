using System.Collections.Generic;

namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record LiveSummary(
    IReadOnlyList<MetaInfo> Metadata,
    ExportSummary Window10s,
    ExportSummary Window1m,
    ExportSummary Window5m,
    ExportSummary Window10m
    );
