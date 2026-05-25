using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Collecting;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Enums;
using Mafi;

namespace CoiTelemetry.RealMod.Aggregation;

public static class SummaryCombiner
{
    public static ExportSummary Combine(IReadOnlyList<ExportSummary> rows, string summaryId, SimStep step)
    {
        var totalTicks = rows.Sum(x => x.Meta.ObservedTicks);
        var meta = new SummaryMeta(summaryId, totalTicks, step, DateTime.UtcNow);

        return new ExportSummary(
            Meta:meta,
            Machines:BuildMachineSummaries(rows),
            Vehicles:BuildVehicleSummaries(rows),
            ProductFlow:BuildProductFlowSummaries(rows)
            );
    }
    private readonly record struct TimedMachineRow(ExportSummary Summary, MachineSummaryRow Machine);
    private static IReadOnlyList<MachineSummaryRow> BuildMachineSummaries(IReadOnlyList<ExportSummary> rows)
    {
        return rows
            .SelectMany(s => s.Machines.Select(m => new TimedMachineRow(s, m)))
            .GroupBy(x => x.Machine.MachineId)
            .Select(g => BuildMachineSummary(g.ToArray()))
            // .OrderByDescending(x=>x.)
            .ToArray();
    }

    private static MachineSummaryRow BuildMachineSummary(IReadOnlyList<TimedMachineRow> rows)
    {
        var latest = rows.Last().Machine;
        var totalTicks = rows.Sum(x => x.Summary.Meta.ObservedTicks);
        double totalSeconds = totalTicks * SimStep.SECONDS_PER_STEP;

        int Sum(Func<MachineSummaryRow, int> selector)
        {
            if (totalTicks <= 0)
            {
                return 0;
            }
            return rows.Sum(x=>selector(x.Machine));
        }

        var inputs = rows
            .SelectMany(x=>x.Machine.Inputs)
            .GroupBy(x=>x.ProductId)
            .Select(g =>
            {
                var amount = g.Sum(x=>x.Amount);
                return new ProductFlowSummary(
                    ProductId: g.Key,
                    Amount: amount,
                    PerMinute: MetricMath.PerMinute(amount, totalSeconds)
                );
            })
            .OrderBy(x=>x.ProductId)
            .ToArray();

        var outputs = rows
            .SelectMany(x=>x.Machine.Outputs)
            .GroupBy(x=>x.ProductId)
            .Select(g =>
            {
                var amount = g.Sum(x => x.Amount);
                return new ProductFlowSummary(
                    ProductId: g.Key,
                    Amount: amount,
                    PerMinute: MetricMath.PerMinute(amount, totalSeconds));
            })
            .OrderBy(x=>x.ProductId)
            .ToArray();

        Dictionary<MachineObservedState, int> uptimeTicks = new();

        foreach (var value in Enum.GetValues(typeof(MachineObservedState)))
        {
            var sum = Sum(m => m.UptimeTicks.TryGetValue((MachineObservedState)value, out var v) ? v : 0);
            if (sum != 0)
            {
                uptimeTicks[(MachineObservedState)value] = sum;
            }
        }

        var primaryBlocker = uptimeTicks
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();

        return new MachineSummaryRow(
            MachineId: latest.MachineId,
            ObservedTicks: totalTicks,
            RecipeId: latest.RecipeId,
            UptimePercent: MetricMath.Percent(uptimeTicks, totalTicks),
            UptimeTicks: uptimeTicks,
            Inputs: inputs,
            Outputs: outputs,
            InputBuffers: latest.InputBuffers,
            OutputBuffers: latest.OutputBuffers,
            PrimaryBlocker: primaryBlocker.Value<=0 ? MachineObservedState.None : primaryBlocker.Key
            );
    }

    private readonly record struct TimedVehicleRow(ExportSummary Summary, VehicleSummaryRow Vehicle);

    private static IReadOnlyList<VehicleSummaryRow> BuildVehicleSummaries(IReadOnlyList<ExportSummary> rows)
    {
        return rows
            .SelectMany(s => s.Vehicles.Select(m => new TimedVehicleRow(s, m)))
            .GroupBy(x => x.Vehicle.VehicleId)
            .Select(g => BuildVehicleSummary(g.ToArray()))
            // .OrderByDescending(x=>x.)
            .ToArray();
    }

    private static VehicleSummaryRow BuildVehicleSummary(IReadOnlyList<TimedVehicleRow> rows)
    {
        var latest = rows.Last().Vehicle;
        var totalTicks = rows.Sum(x => x.Summary.Meta.ObservedTicks);
        double totalSeconds = totalTicks * SimStep.SECONDS_PER_STEP;

        int Sum(Func<VehicleSummaryRow, int> selector)
        {
            if (totalTicks <= 0)
            {
                return 0;
            }
            return rows.Sum(x=>selector(x.Vehicle));
        }

        var delivered = rows
            .SelectMany(x=>x.Vehicle.Delivered)
            .GroupBy(x=>x.ProductId)
            .Select(g =>
            {
                var amount = g.Sum(x=>x.Amount);
                return new ProductFlowSummary(
                    ProductId: g.Key,
                    Amount: amount,
                    PerMinute: MetricMath.PerMinute(amount, totalSeconds));
            })
            .OrderBy(x=>x.ProductId)
            .ToArray();

        var produced = rows
            .SelectMany(x=>x.Vehicle.Produced)
            .GroupBy(x=>x.ProductId)
            .Select(g =>
            {
                var amount = g.Sum(x => x.Amount);
                return new ProductFlowSummary(
                    ProductId:g.Key,
                    Amount:amount,
                    PerMinute:MetricMath.PerMinute(amount, totalSeconds));
            })
            .OrderBy(x=>x.ProductId)
            .ToArray();

        var consumed = rows
            .SelectMany(x=>x.Vehicle.Consumed)
            .GroupBy(x=>x.ProductId)
            .Select(g =>
            {
                var amount = g.Sum(x => x.Amount);
                return new ProductFlowSummary(
                    ProductId:g.Key,
                    Amount:amount,
                    PerMinute:MetricMath.PerMinute(amount, totalSeconds));
            })
            .OrderBy(x=>x.ProductId)
            .ToArray();

        var deliveries = rows.Sum(x=>x.Vehicle.DeliveriesCompleted);
        var fuel = rows.Sum(x=>x.Vehicle.FuelConsumed);
        Dictionary<VehicleObservedState, int> uptimeTicks = new();

        foreach (var value in Enum.GetValues(typeof(VehicleObservedState)))
        {
            var sum = Sum(m => m.UptimeTicks.TryGetValue((VehicleObservedState)value, out var v) ? v : 0);
            if (sum != 0)
            { 
                uptimeTicks[(VehicleObservedState)value] = sum;
            }
        }
        Dictionary<string, int> jobs = new();
        foreach (var row in rows)
        {
            foreach (var kv in row.Vehicle.Jobs)
            {
                jobs[kv.Key] = jobs.TryGetValue(kv.Key, out var v) ? v + kv.Value : kv.Value;
            }
        }
        var primaryBlocker = uptimeTicks
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();
        return new VehicleSummaryRow(
            VehicleId: latest.VehicleId,
            AssignedTo: latest.AssignedTo,
            ObservedTicks: totalTicks,
            DeliveriesCompleted: deliveries,
            FuelConsumed: fuel,
            Delivered: delivered,
            Produced: produced,
            Consumed: consumed,
            PrimaryBlocker: primaryBlocker.Value<=0 ? VehicleObservedState.None : primaryBlocker.Key,
            UptimePercent: MetricMath.Percent(uptimeTicks, totalTicks),
            UptimeTicks: uptimeTicks,
            Jobs: jobs,
            EmptyTravelDistance: rows.Sum(x=>x.Vehicle.EmptyTravelDistance),
            LoadedTravelDistance: rows.Sum(x=>x.Vehicle.LoadedTravelDistance)
        );
    }
    
    private readonly record struct TimedProductFlowRow(ExportSummary Summary, ProductFlowSummaryRow ProductFlow);

    private static IReadOnlyList<ProductFlowSummaryRow> BuildProductFlowSummaries(IReadOnlyList<ExportSummary> rows)
    {
        return rows
            .SelectMany(s => s.ProductFlow.Select(m => new TimedProductFlowRow(s, m)))
            .GroupBy(x => x.ProductFlow.ProductId)
            .Select(g => BuildProductFlowSummaryRows(g.ToArray()))
            // .OrderByDescending(x=>x.)
            .ToArray();
    }
    private static ProductFlowSummaryRow BuildProductFlowSummaryRows(IReadOnlyList<TimedProductFlowRow> rows)
    {
        var latest = rows.Last().ProductFlow;
        var totalTicks = rows.Sum(x => x.Summary.Meta.ObservedTicks);
        double totalSeconds = totalTicks * SimStep.SECONDS_PER_STEP;

        var produced = rows.Sum(x => x.ProductFlow.ProducedAmount);
        var consumed = rows.Sum(x => x.ProductFlow.ConsumedAmount);
        var imported = rows.Sum(x => x.ProductFlow.ImportedAmount);
        var exported = rows.Sum(x => x.ProductFlow.ExportedAmount);
        var mined = rows.Sum(x => x.ProductFlow.MinedAmount);
        var dumped = rows.Sum(x => x.ProductFlow.DumpedAmount);
        var lost = rows.Sum(x => x.ProductFlow.LostAmount);
        var net = produced + mined + imported - consumed - exported - dumped - lost;
        var netPerMinute = MetricMath.PerMinute(net, totalSeconds);

        return new ProductFlowSummaryRow(
            ProductId: latest.ProductId,
            ObservedTicks: totalTicks,
            LatestStored: latest.LatestStored,
            LatestCapacity: latest.LatestCapacity,
            LatestFillPercent: latest.LatestFillPercent,
            MinStored: rows.Min(x => x.ProductFlow.MinStored),
            MaxStored: rows.Max(x => x.ProductFlow.MaxStored),
            AvgStored: rows.Average(x => x.ProductFlow.AvgStored),

            ProducedAmount: produced,
            ConsumedAmount: consumed,
            ImportedAmount: imported,
            ExportedAmount: exported,
            MinedAmount: mined,
            DumpedAmount: dumped,
            LostAmount: lost,
            NetAmount: net,
            ProducedPerMinute: MetricMath.PerMinute(produced, totalSeconds),
            ConsumedPerMinute: MetricMath.PerMinute(consumed, totalSeconds),
            NetPerMinute: netPerMinute,

            EstimatedMinutesUntilEmpty: MetricMath.EstimateMinutesUntilEmpty(latest.LatestStored, netPerMinute),
            EstimatedMinutesUntilFull: MetricMath.EstimateMinutesUntilFull(latest.LatestStored, latest.LatestCapacity,
                netPerMinute)

        );
    }
}