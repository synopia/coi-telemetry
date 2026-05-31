using Mafi.Core.Products;

namespace CoiTelemetry.RealMod.Contracts.Ids;

public readonly record struct ProductId(string Value)
{
    public override string ToString() => Value;
}