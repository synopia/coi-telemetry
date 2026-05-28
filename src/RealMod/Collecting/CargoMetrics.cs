using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Ids;

namespace CoiTelemetry.RealMod.Collecting;

public sealed class CargoMetrics
{
    private const double Epsilon = 0.0000000001;
    private static readonly int DeltaIndex = 2;
    private readonly Dictionary<ProductId, double>[] _buffers = [new(), new(), new()];
    private int _index;
    private bool _initialized;
    public bool IsEmpty => _buffers[_index].Count == 0;

    public double GetCurrent(ProductId productId)
    {
        return _buffers[_index].TryGetValue(productId, out var value) ? value : 0;
    }
    public double GetLast(ProductId productId)
    {
        return _buffers[1 - _index].TryGetValue(productId, out var value) ? value : 0;
    }
    public double GetDelta(ProductId productId)
    {
        return _buffers[DeltaIndex].TryGetValue(productId, out var value) ? value : 0;
    }
    public void Set(ProductId productId, double amount)
    {
        if (amount <= Epsilon)
        {
            return;
        }
        _buffers[_index][productId] = amount;
    }

    public IEnumerable<KeyValuePair<ProductId, double>> GetDelta()
    {
        if (!_initialized)
        {
            _initialized = true;
            return Enumerable.Empty<KeyValuePair<ProductId, double>>();
        }
        return _buffers[DeltaIndex].AsEnumerable();
    }

    public void Reset()
    {
        foreach (var buffer in _buffers)
        {
            buffer.Clear();
        }

        _index = 0;
        _initialized = false;
    }
    
    public void SwapBuffers()
    {
        var other = 1 - _index;
        
        var keys = _buffers[other].Keys.Concat(_buffers[_index].Keys).Distinct();
        _buffers[DeltaIndex].Clear();
        foreach (var key in keys)
        {
            _buffers[_index].TryGetValue(key, out var current);
            _buffers[other].TryGetValue(key, out var last);
            var diff = current - last;
            if (Math.Abs(diff) > Epsilon)
            {
                _buffers[DeltaIndex][key] = diff;
            }
        }
        
        _index = other;
        _buffers[_index].Clear();
    }
}
