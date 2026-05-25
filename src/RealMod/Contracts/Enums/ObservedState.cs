namespace CoiTelemetry.RealMod.Contracts.Enums;

public enum ObservedState
{
    Unknown,
    Working,
    Waiting,
    NotEnoughWorkers,
    NotEnoughPower,// fuel==power
    NotEnoughComputing,
    NotEnoughMaintenance,
    NotEnoughInput, // waiting empty
    OutputFull  // waiting loaded
}