using System;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Aggregation;
using CoiTelemetry.RealMod.Collecting;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Web;
using Mafi;
using Mafi.Core.Entities;
using Mafi.Core.Simulation;

namespace CoiTelemetry.RealMod.Runtime;

public sealed class ExportScheduler :  IDisposable
{
    private readonly MetricsCollector _collector;
    private readonly ISimLoopEvents _events;
    private readonly IModContext _context;
    private readonly RollingSummaryAggregator _aggregator;
    private Duration _exportInterval = Duration.FromSec(10);
    private SimStep _lastExport;
    private LiveDataHub _liveData;
    private ModWebserver _webServer;

    public ExportScheduler(IModContext context, IEntitiesManager entitiesManager,ISimLoopEvents events)
    {
        _events = events;
        _context = context;
        _collector = new MetricsCollector(entitiesManager, _events);
        _aggregator = new RollingSummaryAggregator(Duration.FromMin(10));
        
        _liveData = new LiveDataHub();
        _webServer = new ModWebserver(context,_liveData);
        _webServer.Start();
    }

    public void OnSimulationTick()
    {
        if (_events.IsSimPaused)
        {
            return;
        }
        _collector.ObserveSimulationTick();
        var now = _events.CurrentStep;
        var elapsed = now - _lastExport;
        if (elapsed < _exportInterval)
        {
            return;
        }

        var summary10s = _collector.BuildSummary();
        _aggregator.Add(summary10s);

        var liveSummary = new LiveSummary(
            Window10s: summary10s,
            Window1m: _aggregator.Build(Duration.FromMin(1)),
            Window5m: _aggregator.Build(Duration.FromMin(5)),
            Window10m: _aggregator.Build(Duration.FromMin(10))
        );
        _liveData.UpdateLatest(liveSummary);
        
        _collector.ResetWindowCounters();
        _lastExport = now;
    }

    public void Dispose()
    {
        _webServer.Dispose();
        
        // _liveData.Dispose();
        // _collector.Dispose();
    }
}