using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Ids;
using Mafi;

namespace CoiTelemetry.RealMod.Collecting;

public enum ProductFlowDirection
{
    None,
    Produced,
    Consumed
}
public interface IProductFlowMetrics
{
    void AddProduced(ProductId productId, double amount);
    void AddConsumed(ProductId productId, double amount);
}

public class ProductFlowMetrics
{
    private readonly ProductId _productId;
    public ProductId ProductId => _productId;

    private double _producedAmount;
    private double _consumedAmount;
    private double _importedAmount;
    private double _exportedAmount;
    private double _minedAmount;
    private double _dumpedAmount;
    private double _lostAmount;
    
    public double NetAmount =>
        _producedAmount + _importedAmount + _minedAmount - _consumedAmount - _exportedAmount - _dumpedAmount - _lostAmount;

    public ProductFlowMetrics(ProductId productId)
    {
        _productId = productId;
    }


    public void ResetWindow()
    {
        _producedAmount = 0;
        _consumedAmount = 0;
        _importedAmount = 0;
        _exportedAmount = 0;
        _minedAmount = 0;
        _dumpedAmount = 0;
        _lostAmount = 0;
    }
    
    public void AddProduced(double amount)
    {
        if (amount > 0)
        {
            _producedAmount += amount;
        }
    }
    public void AddConsumed(double amount)
    {
        if (amount > 0)
        {
            _consumedAmount += amount;
        }
    }
    public void AddImported(double amount)
    {
        if (amount > 0)
        {
            _importedAmount+= amount;
        }
    }

    public void AddExported(double amount)
    {
        if (amount > 0)
        {
            _exportedAmount+= amount;
        }
    }
    public void AddMined(double amount)
    {
        if (amount > 0)
        {
            _minedAmount += amount;
        }
    }
    public void AddDumped(double amount)
    {
        if (amount > 0)
        {
            _dumpedAmount += amount;
        }
    }

    public void AddLost(double amount)
    {
        if (amount > 0)
        {
            _lostAmount+= amount;
        }
    }

    public ProductFlowSummaryRow BuildSummaryRow(ProductStorage storage, int observedTicks)
    {
        var windowSeconds = SimStep.SECONDS_PER_STEP * observedTicks;

        var netPerMinute = MetricMath.PerMinute(NetAmount, windowSeconds);
        return new ProductFlowSummaryRow(
            ProductId: _productId.Value,
            ObservedTicks: observedTicks,
            ProducedAmount: _producedAmount,
            ConsumedAmount: _consumedAmount,
            ImportedAmount: _importedAmount,
            ExportedAmount: _exportedAmount,
            MinedAmount: _minedAmount,
            DumpedAmount: _dumpedAmount,
            LostAmount: _lostAmount,

            NetAmount: NetAmount,
            ProducedPerMinute: MetricMath.PerMinute(_producedAmount, windowSeconds),
            ConsumedPerMinute: MetricMath.PerMinute(_consumedAmount, windowSeconds),
            NetPerMinute: netPerMinute,
            LatestStored: storage.Stored,
            LatestCapacity: storage.Capacity,
            LatestFillPercent: 100 * storage.Stored / storage.Capacity,
            MinStored: storage.Stored,
            MaxStored: storage.Stored,
            AvgStored: storage.Stored,

            EstimatedMinutesUntilEmpty: MetricMath.EstimateMinutesUntilEmpty(storage.Stored, netPerMinute),
            EstimatedMinutesUntilFull: MetricMath.EstimateMinutesUntilFull(storage.Stored, storage.Capacity,
                netPerMinute)
        );
    }
}