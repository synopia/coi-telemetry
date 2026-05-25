namespace CoiTelemetry.RealMod.Contracts.Dtos;

public sealed record ProductFlowSummaryRow(
    string ProductId,
    int ObservedTicks,
    double LatestStored,
    double LatestCapacity,
    double LatestFillPercent,
    
    double MinStored,
    double MaxStored,
    double AvgStored,
    
    double ProducedAmount,
    double ConsumedAmount,
    double ImportedAmount,
    double ExportedAmount,
    double MinedAmount,
    double DumpedAmount,
    double LostAmount,
    double NetAmount,
        double ProducedPerMinute,
    double ConsumedPerMinute,
    double NetPerMinute,
    double? EstimatedMinutesUntilEmpty,
    double? EstimatedMinutesUntilFull
    );
