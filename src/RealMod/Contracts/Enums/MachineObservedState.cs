namespace CoiTelemetry.RealMod.Contracts.Enums;

public enum MachineObservedState
{
    None,
    Broken,
    Paused,
    NotEnoughWorkers,
    NotEnoughPower,
    NotEnoughComputing,
    NotEnoughInput,
    InvalidPlacement,
    OutputFull,
    NoRecipes,
    Working
}