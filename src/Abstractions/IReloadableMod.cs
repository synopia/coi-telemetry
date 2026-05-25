using System;

namespace CoiTelemetry.Abstractions
{
    public interface IReloadableMod : IDisposable
    {
        void Start(IModContext context);
        void Stop();
        void OnSimulationTick();
    }
}