using System;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CoiTelemetry.Abstractions;
using CoiTelemetry.RealMod.Collecting;
using CoiTelemetry.RealMod.Web;
using Mafi;
using Mafi.Core.Entities;
using Mafi.Core.Prototypes;
using Mafi.Core.Simulation;
using Mafi.Unity;
using UnityEngine;

namespace CoiTelemetry.RealMod.Runtime
{
    public class ModRuntime : IDisposable
    {
        private readonly IModContext _context;
        private readonly ModWebserver _webServer;
        private readonly ExportScheduler _scheduler ;
        
        public ModRuntime(IModContext context)
        {
            _context = context;
            var entitiesManager = context.Resolver.Resolve<IEntitiesManager>();
            var events = context.Resolver.Resolve<ISimLoopEvents>();
            var main = context.Resolver.Resolve<IMain>();
            var db = context.Resolver.Resolve<ProtosDb>();
            
            _webServer = new ModWebserver(context, main.AssetsDb);
            
            _scheduler = new ExportScheduler(context,db,entitiesManager, events, _webServer);
            
        }
        
        public void Start()
        {
            _webServer.Start();
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
            _webServer.Dispose();
            _scheduler.Dispose();
        }
    }
}