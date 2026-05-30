using System;
using System.Reflection;
using System.Text;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Aggregation;
using CoiTelemetry.RealMod.Collecting;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Web;
using Mafi;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Simulation;
using Mafi.Serialization;

namespace CoiTelemetry.RealMod.Runtime;

public sealed class ExportScheduler :  IDisposable
{

    private readonly MetricsCollector _collector;
    private readonly ISimLoopEvents _events;
    private readonly IModContext _context;
    private Duration _exportInterval = Duration.FromSec(1);
    private SimStep _lastExport;
    private LiveDataHub _liveData;
    private ModWebserver _webServer;
    private readonly IEntitiesManager _entitiesManager;
    private readonly AggregationWorker _aggregator ;
    private DateTime _lastDebug = DateTime.UtcNow;

    
    public ExportScheduler(IModContext context, IEntitiesManager entitiesManager,ISimLoopEvents events)
    {
        _entitiesManager = entitiesManager;
        _events = events;
        _context = context;
        _collector = new MetricsCollector(context, entitiesManager, _events);
        
        _liveData = new LiveDataHub();
        _aggregator = new(context, _liveData);
        _webServer = new ModWebserver(context,_liveData);
        _webServer.Start();
    }

    public void OnSimulationTick()
    {
        if (_events.IsSimPaused)
        {
            return;
        }
        SimProfiler.NewFrame();

        using (SimProfiler.Scope("OnSimulationTick"))
        {
            using (SimProfiler.Scope("ObserveSimulationTick"))
            {
                _collector.ObserveSimulationTick();
            }

            var now = _events.CurrentStep;
            var elapsed = now - _lastExport;
            if (elapsed < _exportInterval)
            {
                return;
            }

            using (SimProfiler.Scope("Summary"))
            {

                ExportSummary summary10s;
                using (SimProfiler.Scope("BuildSummary"))
                {
                    summary10s = _collector.BuildSummary(includeNetworkAnalysis: false, includeMetadata:true);
                }

                using (SimProfiler.Scope("ResetWindowCounters"))
                {
                    _collector.ResetWindowCounters();
                }

                _aggregator.TryEnqueue(summary10s);

            }

            _lastExport = now;
         
        }
        if (DateTime.UtcNow - _lastDebug > TimeSpan.FromSeconds(10))
        {
            _lastDebug = DateTime.UtcNow;
            _context.Logger.Info(SimProfiler.Dump());
        }

    }

    public void Dispose()
    {
        _aggregator.Dispose();
        _webServer.Dispose();
        _collector.Dispose();
        
        // _liveData.Dispose();
    }
}
