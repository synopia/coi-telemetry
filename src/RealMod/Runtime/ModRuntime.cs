using System;
using System.Threading;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Collecting;
using Mafi;
using Mafi.Core.Entities;
using Mafi.Core.Simulation;

namespace CoiTelemetry.RealMod.Runtime
{
    public class ModRuntime : IDisposable
    {
        private readonly IModContext _context;
        private readonly ExportScheduler _scheduler ;
        
        public ModRuntime(IModContext context)
        {
            _context = context;
            var entitiesManager = context.Resolver.Resolve<IEntitiesManager>();
            var events = context.Resolver.Resolve<ISimLoopEvents>();
            _scheduler = new ExportScheduler(context,entitiesManager, events);
            
        }
        
        public void Start()
        {
        }
        
        public void OnSimulationTick()
        {
            try
            {
                _context.Logger.Info(Thread.CurrentThread.Name??"Unknown");
                _scheduler.OnSimulationTick();
            }catch (Exception e)
            {
                _context.Logger.Error(e);
            }
        }

        public void Dispose()
        {
            _scheduler.Dispose();
        }
    }
}