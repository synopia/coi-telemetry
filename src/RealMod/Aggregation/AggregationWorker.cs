using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Runtime;
using CoiTelemetry.RealMod.Web;
using Mafi;

namespace CoiTelemetry.RealMod.Aggregation;


public sealed class AggregationWorker : IDisposable
{
    private static readonly Duration Window1mRefreshInterval = Duration.FromSec(2);
    private static readonly Duration Window5mRefreshInterval = Duration.FromSec(10);
    private static readonly Duration Window10mRefreshInterval = Duration.FromSec(5);

    private readonly IModContext _context;
    private readonly HierarchicalProfiler _profiler = new("COI.Mod.AggregationWorker");
    private readonly BlockingCollection<ExportSummary> _queue;
    private readonly Thread _thread;
    private readonly int _maxQueuedJobs;
    private volatile bool _disposed;
    private readonly RollingSummaryAggregator _aggregator;
    private readonly LiveDataHub _liveData;
    private ExportSummary? _cachedWindow1m;
    private ExportSummary? _cachedWindow5m;
    private ExportSummary? _cachedWindow10m;
    private SimStep _lastWindow1mBuild;
    private SimStep _lastWindow5mBuild;
    private SimStep _lastWindow10mBuild;
    private DateTime _lastDebug = DateTime.UtcNow;
    
    
    public AggregationWorker(IModContext context,LiveDataHub liveData, int maxQueuedJobs = 5)
    {
        _context = context;
        _aggregator = new RollingSummaryAggregator(Duration.FromMin(10));
        _liveData = liveData;
        _maxQueuedJobs = maxQueuedJobs;
        _queue = new BlockingCollection<ExportSummary>(
            new ConcurrentQueue<ExportSummary>(),
            boundedCapacity:maxQueuedJobs
            );

        _thread = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = "CoiTelemetry.Aggregation.Worker"
        };
        _thread.Start();
    }

    public bool TryEnqueue(ExportSummary summary)
    {
        if (_disposed)
        {
            return false;
        }

        if (_queue.TryAdd(summary))
        {
            return true;
        }
        _queue.TryTake(out _);
        
        return _queue.TryAdd(summary);
    }

    private void ProcessQueue()
    {
        foreach (var summary in _queue.GetConsumingEnumerable())
        {
            try
            {
                _profiler.NewFrame();
                using (_profiler.Scope("AggregationWorker"))
                {
                    using (_profiler.Scope("AddSummary"))
                    {
                        _aggregator.Add(summary);
                    }

                    var now = summary.Meta.Step;

                    ExportSummary window1m;
                    using (_profiler.Scope("BuildWindow1m"))
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
                    using (_profiler.Scope("BuildWindow5m"))
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
                    using (_profiler.Scope("BuildWindow10m"))
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
                        Metadata: summary.Metadata,
                        Window10s: summary.WithoutMetadata(),
                        Window1m: window1m,
                        Window5m: window5m,
                        Window10m: window10m
                    );
                    using (_profiler.Scope("UpdateLatest"))
                    {
                        _liveData.UpdateLatest(liveSummary);
                    }

                }

                if (DateTime.UtcNow - _lastDebug > TimeSpan.FromSeconds(10))
                {
                    _lastDebug = DateTime.UtcNow;
                    // _context.Logger.Info(_profiler.Dump());
                    _profiler.Dump();
                }
            }
            catch (Exception e)
            {
                _context.Logger.Error(e);
            }
        }
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

    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _queue.CompleteAdding();
        _thread.Join(millisecondsTimeout:5000);
        _queue.Dispose();
    }
}