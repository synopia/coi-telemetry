using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Dtos;
using Mafi;

namespace CoiTelemetry.RealMod.Aggregation;

public sealed class RollingSummaryAggregator
{
    private readonly Queue<ExportSummary> _summaries = new();
    private readonly Duration _maxWindow;

    public RollingSummaryAggregator(Duration maxWindow)
    {
        _maxWindow = maxWindow;
    }

    public void Add(ExportSummary summary)
    {
        _summaries.Enqueue(summary);
        var cutoff = summary.Meta.Step - _maxWindow;
        while (_summaries.Count > 0 && _summaries.Peek().Meta.Step < cutoff)
        {
            _summaries.Dequeue();
        }
    }

    public ExportSummary Build(Duration window)
    {
        var now = _summaries.Last().Meta.Step;
        var cutoff = now - window;
        var rows = _summaries
            .Where(x => x.Meta.Step > cutoff)
            .ToArray();

        return SummaryCombiner.Combine(rows, $"{window.Seconds}s_{now.Value:D12}", now);
    }
}