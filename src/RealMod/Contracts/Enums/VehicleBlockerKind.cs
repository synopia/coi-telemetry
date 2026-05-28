namespace CoiTelemetry.RealMod.Contracts.Enums;

public enum VehicleBlockerKind
{
    None,
    Unknown,
    NoJob,
    GoalUnreachable,
    NotEnoughMaintenance,
    NotEnoughWorkers,
    NeedsFuel,
    RefuelRequestFailed,
    RefuelUnreachable,
    NotEnoughComputing,
    PathFinding,
    WaitingForRoadExit,
    Stuck,
    StrugglingToNavigate,
    CannotDeliverCargo,
    WaitingForUnload,
    WaitingForPickup,
    NoHarvestTarget,
    NoTruckAvailable,
    WaitingForTruck
}
