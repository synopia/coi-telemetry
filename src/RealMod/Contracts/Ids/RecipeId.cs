namespace CoiTelemetry.RealMod.Contracts.Ids;


public readonly record struct RecipeId(string Value)
{
    public override string ToString() => Value;
}
