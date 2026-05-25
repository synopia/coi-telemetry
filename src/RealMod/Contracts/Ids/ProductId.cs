using Mafi.Core.Products;

namespace CoiTelemetry.RealMod.Contracts.Ids;

public readonly record struct ProductId(string Value, ProductProto.ID CoiId)
{
    public override string ToString() => Value;
}