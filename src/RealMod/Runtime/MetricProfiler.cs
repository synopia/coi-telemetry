namespace CoiTelemetry.RealMod.Runtime;
using System;
using System.Diagnostics;

public sealed class MetricCollectionProfiler
{
    private readonly Stopwatch _wallClock = Stopwatch.StartNew();

    private long _collectionTicks;
    private long _collectionCalls;

    public IDisposable Measure()
    {
        return new Scope(this);
    }

    public MetricProfilerSnapshot GetSnapshotAndReset()
    {
        var wallSeconds = _wallClock.Elapsed.TotalSeconds;
        var collectionSeconds = (double)_collectionTicks / Stopwatch.Frequency;

        var percent = wallSeconds <= 0
            ? 0
            : collectionSeconds / wallSeconds * 100.0;

        var result = new MetricProfilerSnapshot(
            WallSeconds: wallSeconds,
            CollectionSeconds: collectionSeconds,
            CollectionCalls: _collectionCalls,
            CollectionCpuPercentApprox: percent,
            AvgCollectionMs: _collectionCalls <= 0
                ? 0
                : collectionSeconds * 1000.0 / _collectionCalls
        );

        _wallClock.Restart();
        _collectionTicks = 0;
        _collectionCalls = 0;

        return result;
    }

    private void Add(long elapsedTicks)
    {
        _collectionTicks += elapsedTicks;
        _collectionCalls++;
    }

    private sealed class Scope : IDisposable
    {
        private readonly MetricCollectionProfiler _owner;
        private readonly long _start;
        private bool _disposed;

        public Scope(MetricCollectionProfiler owner)
        {
            _owner = owner;
            _start = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            var elapsed = Stopwatch.GetTimestamp() - _start;
            _owner.Add(elapsed);
        }
    }
}

public sealed record MetricProfilerSnapshot(
    double WallSeconds,
    double CollectionSeconds,
    long CollectionCalls,
    double CollectionCpuPercentApprox,
    double AvgCollectionMs
);