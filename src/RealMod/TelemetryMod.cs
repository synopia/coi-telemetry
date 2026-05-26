using System.Diagnostics;
using System.Threading;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Runtime;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}
namespace CoiTelemetry.RealMod
{
    public class TelemetryMod: IReloadableMod
    {
        private ModRuntime? _runtime;

        public void Start(IModContext context)
        {
            _runtime = new ModRuntime(context);
            _runtime.Start();
            
            context.Logger.Info("Telemetry mod started");
        }

        public void OnSimulationTick()
        {
            _runtime?.OnSimulationTick();
        }

        public void Stop()
        {
            _runtime?.Dispose();
            _runtime = null;
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}