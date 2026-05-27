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
    private readonly RollingSummaryAggregator _aggregator;
    private Duration _exportInterval = Duration.FromSec(1);
    private SimStep _lastExport;
    private LiveDataHub _liveData;
    private ModWebserver _webServer;
    private readonly IEntitiesManager _entitiesManager;
    private MetricCollectionProfiler _profiler = new ();
    private DateTime _lastDebug = DateTime.UtcNow;
    
    public ExportScheduler(IModContext context, IEntitiesManager entitiesManager,ISimLoopEvents events)
    {
        _entitiesManager = entitiesManager;
        _events = events;
        _context = context;
        _collector = new MetricsCollector(context, entitiesManager, _events);
        _aggregator = new RollingSummaryAggregator(Duration.FromMin(10));
        
        _liveData = new LiveDataHub();
        _webServer = new ModWebserver(context,_liveData);
        _webServer.Start();
    }

    public void OnSimulationTick()
    {
        // Debug();
        if (_events.IsSimPaused)
        {
            return;
        }

        using (_profiler.Measure())
        {
            _collector.ObserveSimulationTick();
        }

        if( DateTime.UtcNow - _lastDebug > TimeSpan.FromSeconds(10))
        {
            _lastDebug = DateTime.UtcNow;
            var perf = _profiler.GetSnapshotAndReset();
            _context.Logger.Info(
                $"Metric collection: {perf.CollectionCpuPercentApprox:F3}% " +
                $"({perf.CollectionSeconds * 1000:F2}ms / {perf.WallSeconds:F2}s, " +
                $"avg {perf.AvgCollectionMs:F4}ms/call, calls {perf.CollectionCalls})"
                );
        }
        
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

    private void Debug()
    {
        var requestEntity = _liveData.RequestEntity;
        
        if (requestEntity != null)
        {
            var sb = new StringBuilder();
            var path = requestEntity.Split('/');
            if (path.Length > 0)
            {
                var id = int.Parse(path[0]);
                var entity = _entitiesManager.GetEntity(new EntityId(id)).Value;
                
                if (entity is not null)
                {
                    _context.Logger.Info(ObjectInspector.Inspect(((Vehicle)entity).CurrentJob, maxDepth:2));
                    var type = entity.GetType();
                    foreach (var property in type.GetFields())
                    {
                        sb.AppendLine($"{property.Name} = {property.GetValue(entity)}");
                    }
                }
            }
            
            _liveData.RequestEntity = null;
            _liveData.ResponseEntity = sb.ToString();
        }
    }

    public void Dispose()
    {
        _webServer.Dispose();
        
        // _liveData.Dispose();
        // _collector.Dispose();
    }
}