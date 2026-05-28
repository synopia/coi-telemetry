namespace CoiTelemetry.RealMod.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

public sealed class HierarchicalProfiler
{
    private readonly Stack<Frame> _stack = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public ProfilerNode Root { get; }

    public int FrameCount { get; private set; }

    public HierarchicalProfiler(string name = "Profiler")
    {
        Root = new ProfilerNode($"{name}.Root", null);
        _stack.Push(new Frame(Root, _stopwatch.ElapsedTicks));
    }

    public void Reset()
    {
        Root.ResetRecursive();
        FrameCount = 0;

        _stack.Clear();
        _stopwatch.Restart();
        _stack.Push(new Frame(Root, _stopwatch.ElapsedTicks));
    }

    public void NewFrame()
    {
        // Close root frame interval for the previous frame.
        long now = _stopwatch.ElapsedTicks;

        if (_stack.Count != 1)
        {
            // You can throw here during development.
            // In production/mod code, it is often safer to auto-close broken scopes.
            while (_stack.Count > 1)
                Stop();
        }

        var rootFrame = _stack.Pop();
        Root.AddSample(now - rootFrame.StartTicks);

        FrameCount++;

        _stack.Push(new Frame(Root, now));
    }

    public void Start(string name)
    {
        var parent = _stack.Peek().Node;
        var node = parent.GetOrCreateChild(name);

        _stack.Push(new Frame(node, _stopwatch.ElapsedTicks));
    }

    public void Stop()
    {
        if (_stack.Count <= 1)
            throw new InvalidOperationException("Cannot stop root profiler scope.");

        long now = _stopwatch.ElapsedTicks;
        var frame = _stack.Pop();

        frame.Node.AddSample(now - frame.StartTicks);
    }

    public ProfileScope Scope(string name)
    {
        Start(name);
        return new ProfileScope(this);
    }

    public string Dump(int maxDepth = 8)
    {
        var sb = new StringBuilder();

        double rootMsPerFrame = Root.TotalMs / Math.Max(1, FrameCount);

        sb.AppendLine(
            $"{Root.Name} {FrameCount} frames " +
            $"{FormatMs(rootMsPerFrame)}/frame {FormatMs(Root.TotalMs)} total");

        foreach (var child in Root.Children.Values.OrderByDescending(c => c.TotalTicks))
        {
            DumpNode(sb, child, Root.TotalTicks, 1, maxDepth);
        }

        double otherTicks = Root.GetOtherTicks();
        if (otherTicks > 0)
            DumpOther(sb, otherTicks, Root.TotalTicks, 1);

        return sb.ToString();
    }

    private void DumpNode(
        StringBuilder sb,
        ProfilerNode node,
        long parentTicks,
        int depth,
        int maxDepth)
    {
        if (depth > maxDepth)
            return;

        string indent = new string(' ', depth * 2);

        double percent = parentTicks <= 0
            ? 0
            : node.TotalTicks * 100.0 / parentTicks;

        double msPerFrame = node.TotalMs / Math.Max(1, FrameCount);
        double callsPerFrame = node.CallCount / (double)Math.Max(1, FrameCount);

        sb.AppendLine(
            $"{indent}{node.Name} " +
            $"{percent:0.0}% " +
            $"{FormatMs(msPerFrame)}/frame " +
            $"{callsPerFrame:0.##} calls/frame " +
            $"({FormatMs(node.MinMs)}/{FormatMs(node.AvgMs)}/{FormatMs(node.MaxMs)})");

        foreach (var child in node.Children.Values.OrderByDescending(c => c.TotalTicks))
        {
            DumpNode(sb, child, node.TotalTicks, depth + 1, maxDepth);
        }

        double otherTicks = node.GetOtherTicks();
        if (otherTicks > 0)
            DumpOther(sb, otherTicks, node.TotalTicks, depth + 1);
    }

    private void DumpOther(
        StringBuilder sb,
        double ticks,
        long parentTicks,
        int depth)
    {
        string indent = new string(' ', depth * 2);

        double totalMs = TicksToMs(ticks);
        double msPerFrame = totalMs / Math.Max(1, FrameCount);

        double percent = parentTicks <= 0
            ? 0
            : ticks * 100.0 / parentTicks;

        sb.AppendLine(
            $"{indent}Other " +
            $"{percent:0.0}% " +
            $"{FormatMs(totalMs)} total " +
            $"{FormatMs(msPerFrame)}/frame");
    }

    private static double TicksToMs(double ticks)
    {
        return ticks * 1000.0 / Stopwatch.Frequency;
    }

    private static string FormatMs(double ms)
    {
        if (ms < 0.001)
            return $"{ms * 1_000_000:0.##}ns";

        if (ms < 1)
            return $"{ms * 1000:0.##}µs";

        return $"{ms:0.###}ms";
    }

    private readonly record struct Frame(ProfilerNode Node, long StartTicks);

    public readonly struct ProfileScope : IDisposable
    {
        private readonly HierarchicalProfiler? _profiler;

        public ProfileScope(HierarchicalProfiler profiler)
        {
            _profiler = profiler;
        }

        public void Dispose()
        {
            _profiler?.Stop();
        }
    }
}

public sealed class ProfilerNode
{
    public string Name { get; }
    public ProfilerNode? Parent { get; }

    public Dictionary<string, ProfilerNode> Children { get; } = new();

    public long TotalTicks { get; private set; }
    public long MinTicks { get; private set; } = long.MaxValue;
    public long MaxTicks { get; private set; }
    public int CallCount { get; private set; }

    public double TotalMs => TotalTicks * 1000.0 / Stopwatch.Frequency;

    public double MinMs => CallCount == 0
        ? 0
        : MinTicks * 1000.0 / Stopwatch.Frequency;

    public double MaxMs => MaxTicks * 1000.0 / Stopwatch.Frequency;

    public double AvgMs => CallCount == 0
        ? 0
        : TotalMs / CallCount;

    public ProfilerNode(string name, ProfilerNode? parent)
    {
        Name = name;
        Parent = parent;
    }

    public ProfilerNode GetOrCreateChild(string name)
    {
        if (!Children.TryGetValue(name, out var child))
        {
            child = new ProfilerNode(name, this);
            Children.Add(name, child);
        }

        return child;
    }

    public void AddSample(long ticks)
    {
        if (ticks < 0)
            ticks = 0;

        TotalTicks += ticks;
        CallCount++;

        if (ticks < MinTicks)
            MinTicks = ticks;

        if (ticks > MaxTicks)
            MaxTicks = ticks;
    }

    public long GetOtherTicks()
    {
        long childTicks = 0;

        foreach (var child in Children.Values)
            childTicks += child.TotalTicks;

        return Math.Max(0, TotalTicks - childTicks);
    }

    public void ResetRecursive()
    {
        TotalTicks = 0;
        MinTicks = long.MaxValue;
        MaxTicks = 0;
        CallCount = 0;

        foreach (var child in Children.Values)
            child.ResetRecursive();
    }
}

public static class Profiler
{
    private static readonly HierarchicalProfiler Instance = new("COI.Mod");

    public static void Reset() => Instance.Reset();

    public static void NewFrame() => Instance.NewFrame();

    public static IDisposable Scope(string name) => Instance.Scope(name);

    public static string Dump(int maxDepth = 8) => Instance.Dump(maxDepth);
    /*
    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }

    public static void Reset() { }

    public static void NewFrame() { }

    public static IDisposable Scope(string name) => NoopScope.Instance;

    public static string Dump(int maxDepth = 8) => "Profiling disabled.";
*/
}