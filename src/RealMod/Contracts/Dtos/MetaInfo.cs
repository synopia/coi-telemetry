using System.Collections.Generic;

namespace CoiTelemetry.RealMod.Contracts.Dtos;

public record struct MetaProto( string Name);
public record struct MetaProduct(string Name, string IconUrl);
public record struct MetaRecipe( string Name);
public record struct MetaEntity( string Name, string ProtoId);

public sealed record Metadata(
    IReadOnlyDictionary<string, MetaProto> Protos,
    IReadOnlyDictionary<string, MetaProduct> Products,
    IReadOnlyDictionary<string, MetaRecipe> Recipes,
    IReadOnlyDictionary<string, MetaEntity> Entities
    );
