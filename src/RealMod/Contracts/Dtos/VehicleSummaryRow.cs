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

    double EmptyTravelDistance,
    double LoadedTravelDistance,
    int DeliveriesCompleted,
    double FuelConsumed,

    IReadOnlyList<ProductFlowSummary> Delivered,
    IReadOnlyList<ProductFlowSummary> Produced,
    IReadOnlyList<ProductFlowSummary> Consumed,

    IReadOnlyDictionary<string,int> Jobs,

    [property:JsonConverter(typeof(StringEnumConverter))]
    ObservedState PrimaryBlocker);