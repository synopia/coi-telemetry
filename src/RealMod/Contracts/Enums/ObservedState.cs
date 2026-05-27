namespace CoiTelemetry.RealMod.Contracts.Enums;

public enum ObservedState
{
    Unknown,
    Working,
    Idle,
    
    NotEnoughInput, // waiting empty
    OutputFull,  // waiting loaded
    
    NotEnoughWorkers,
    NotEnoughPower,// fuel==power
    NotEnoughComputing,
    NotEnoughMaintenance
}

