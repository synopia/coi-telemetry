using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record VehicleSummaryRow(
    string VehicleId,
    int ObservedTicks,

    string? AssignedTo,
    IReadOnlyDictionary<ObservedState, double> UptimePercent,
    IReadOnlyDictionary<ObservedState, int> UptimeTicks,
    IReadOnlyDictionary<VehicleBlockerKind, double> BlockerPercent,
    IReadOnlyDictionary<VehicleBlockerKind, int> BlockerTicks,
    
    int Electricity,
    PressureSummary Pressure,

    double EmptyTravelDistance,
    double LoadedTravelDistance,
    int DeliveriesCompleted,

    IReadOnlyList<ProductFlowSummary> Delivered,
    IReadOnlyList<ProductFlowSummary> Produced,
    IReadOnlyList<ProductFlowSummary> Consumed,

    IReadOnlyDictionary<string,int> Jobs,
    string? CurrentJob,
    string? CurrentJobInfo,
    string? CurrentGoal,
    string PathFindingState,
    string DrivingState,

    [property:JsonConverter(typeof(StringEnumConverter))]
    VehicleBlockerKind PrimaryDetailedBlocker,
    [property:JsonConverter(typeof(StringEnumConverter))]
    ObservedState PrimaryBlocker) : BaseSummaryRow(Electricity, Pressure);
