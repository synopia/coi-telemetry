using System;
using System.Collections.Generic;
using Mafi;
using Newtonsoft.Json;

namespace CoiTelemetry.RealMod.Contracts.Dtos;

public class SimStepConverter : JsonConverter<SimStep>
{
    public override void WriteJson(JsonWriter writer, SimStep value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override SimStep ReadJson(JsonReader reader, Type objectType, SimStep existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var n = reader.ReadAsInt32();
        return new SimStep(n ?? 0);
    }
}
public sealed record SummaryMeta(
    string SummaryId,
    int ObservedTicks,
    [property:JsonConverter(typeof(SimStepConverter))]
    SimStep Step,
    DateTime CreatedAtUtc
    );

public sealed record ExportSummary(
    IReadOnlyList<MetaInfo>? Metadata,
    SummaryMeta Meta,
    IReadOnlyList<MachineSummaryRow> Machines,
    IReadOnlyList<VehicleSummaryRow> Vehicles,
    IReadOnlyList<ProductFlowSummaryRow> ProductFlow,
    ProductDependencyGraph DependencyGraph,
    ProductDependencyImpactSimulation ImpactSimulation)
{
    public ExportSummary WithoutMetadata() =>
        new(null, Meta, Machines, Vehicles, ProductFlow, DependencyGraph, ImpactSimulation);

}
