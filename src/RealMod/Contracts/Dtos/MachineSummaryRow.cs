using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoiTelemetry.RealMod.Contracts.Dtos;


public sealed record MachineSummaryRow(
    string MachineId,
    string? RecipeId,
    int ObservedTicks,

    IReadOnlyDictionary<ObservedState, double> UptimePercent,
    IReadOnlyDictionary<ObservedState, int> UptimeTicks,

    int Electricity,
    PressureSummary Pressure,

    IReadOnlyList<ProductFlowSummary> Inputs,
    IReadOnlyList<ProductFlowSummary> Outputs,

    IReadOnlyList<ProductBufferSummary> InputBuffers,
    IReadOnlyList<ProductBufferSummary> OutputBuffers,
    IReadOnlyList<MachinePotentialScenario> PotentialScenarios,

    [property:JsonConverter(typeof(StringEnumConverter))]
    ObservedState PrimaryBlocker
) : BaseSummaryRow(Electricity, Pressure);
