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
    private static readonly Duration Window1mRefreshInterval = Duration.FromSec(2);
    private static readonly Duration Window5mRefreshInterval = Duration.FromSec(10);
    private static readonly Duration Window10mRefreshInterval = Duration.FromSec(5);

    private readonly MetricsCollector _collector;
    private readonly ISimLoopEvents _events;
    private readonly IModContext _context;
    private readonly RollingSummaryAggregator _aggregator;
    private Duration _exportInterval = Duration.FromSec(1);
    private SimStep _lastExport;
    private LiveDataHub _liveData;
    private ModWebserver _webServer;
    private readonly IEntitiesManager _entitiesManager;
    private DateTime _lastDebug = DateTime.UtcNow;
    private ExportSummary? _cachedWindow1m;
    private ExportSummary? _cachedWindow5m;
    private ExportSummary? _cachedWindow10m;
    private SimStep _lastWindow1mBuild;
    private SimStep _lastWindow5mBuild;
    private SimStep _lastWindow10mBuild;
    
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
        Profiler.NewFrame();

        using (Profiler.Scope("ObserveSimulationTick"))
        {
            _collector.ObserveSimulationTick();
        }
        
        var now = _events.CurrentStep;
        var elapsed = now - _lastExport;
        if (elapsed < _exportInterval)
        {
            return;
        }
        using (Profiler.Scope("Frame"))
        {

            ExportSummary summary10s;
            using (Profiler.Scope("BuildSummary"))
            {
                summary10s = _collector.BuildSummary(includeNetworkAnalysis: false);
            }

            using (Profiler.Scope("AddSummary"))
            {
                _aggregator.Add(summary10s);
            }

            ExportSummary window1m;
            using (Profiler.Scope("BuildWindow1m"))
            {
                window1m = BuildCachedWindow(
                    window: Duration.FromMin(1),
                    currentStep: now,
                    refreshInterval: Window1mRefreshInterval,
                    includeNetworkAnalysis: false,
                    ref _cachedWindow1m,
                    ref _lastWindow1mBuild);
            }
            ExportSummary window5m;
            using (Profiler.Scope("BuildWindow5m"))
            {
                window5m = BuildCachedWindow(
                    window: Duration.FromMin(5),
                    currentStep: now,
                    refreshInterval: Window5mRefreshInterval,
                    includeNetworkAnalysis: false,
                    ref _cachedWindow5m,
                    ref _lastWindow5mBuild);
            }

            ExportSummary window10m;
            using (Profiler.Scope("BuildWindow10m"))
            {
                window10m = BuildCachedWindow(
                    window: Duration.FromMin(10),
                    currentStep: now,
                    refreshInterval: Window10mRefreshInterval,
                    includeNetworkAnalysis: true,
                    ref _cachedWindow10m,
                    ref _lastWindow10mBuild);
            }
            
            var liveSummary = new LiveSummary(
                Metadata: _collector.BuildMetadata(),
                Window10s: summary10s,
                Window1m: window1m,
                Window5m: window5m,
                Window10m: window10m
            );
            using (Profiler.Scope("UpdateLatest"))
            {
                // _liveData.UpdateLatest(liveSummary);
            }

            using (Profiler.Scope("ResetWindowCounters"))
            {
                _collector.ResetWindowCounters();
            }
            
        }

        if (DateTime.UtcNow - _lastDebug > TimeSpan.FromSeconds(10))
        {
            _lastDebug = DateTime.UtcNow;
            _context.Logger.Info(Profiler.Dump());
        }
        _lastExport = now;
    }

    private ExportSummary BuildCachedWindow(
        Duration window,
        SimStep currentStep,
        Duration refreshInterval,
        bool includeNetworkAnalysis,
        ref ExportSummary? cachedSummary,
        ref SimStep lastBuildStep)
    {
        if (cachedSummary is not null && currentStep - lastBuildStep < refreshInterval)
        {
            return cachedSummary;
        }

        cachedSummary = _aggregator.Build(window, includeNetworkAnalysis);
        lastBuildStep = currentStep;
        return cachedSummary;
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
        _collector.Dispose();
        
        // _liveData.Dispose();
    }
}
