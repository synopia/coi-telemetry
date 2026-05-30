namespace CoiTelemetry.RealMod.Contracts.Dtos;

public record struct PressureSummary(double? Maintenance, double? Power, double? Computing, double? Workers);
public abstract record BaseSummaryRow(int Electricity, PressureSummary Pressure);