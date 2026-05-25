namespace CoiTelemetry.RealMod.Contracts.Ids;


public readonly record struct EntityId(string Value)
{
    public override string ToString() => Value;
}