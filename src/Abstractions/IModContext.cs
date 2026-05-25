using Mafi;

namespace CoiTelemetry.Abstractions
{
    public interface IModContext
    {
        IModLogger Logger { get; }
        string ModDirectory { get; }
        string RealModDirectory { get; }
        string DataDirectory { get; }
        DependencyResolver Resolver { get; }
    }
}