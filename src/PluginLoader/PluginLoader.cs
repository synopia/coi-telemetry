using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CoiTelemetry.Abstractions;
using Mafi;
using Mafi.Core.Mods;
using Mafi.Core.Simulation;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}

namespace CoiTelemetry.PluginLoader
{

    public class PluginLogger : IModLogger
    {
        public void Info(string message)
        {
            Log.Info($"[CoiTelemetry] {message}");
        }

        public void Warn(string message)
        {
            Log.Warning($"[CoiTelemetry] {message}");
        }

        public void Error(string message)
        {
            Log.Error($"[CoiTelemetry] {message}");
        }

        public void Error(Exception ex)
        {
            Log.Error($"[CoiTelemetry] {ex}");
        }
    }

    public sealed record ModContext(
        IModLogger Logger,
        string ModDirectory,
        string RealModDirectory,
        string DataDirectory,
        DependencyResolver Resolver) : IModContext;

    public class PluginLoader : DataOnlyMod, IMod, IDisposable
    {
        private readonly PluginLogger _logger = new();
        private DependencyResolver _resolver = null!;
        private ISimLoopEvents _events = null!;
        private string _path;
        private DateTime _lastWriteUtc;
        private ModManifest _manifest;
        private IReloadableMod? _current;
        private IModContext _context;
        
        public PluginLoader(ModManifest manifest) : base(manifest)
        {
            _manifest = manifest;
            _path = Path.Combine(manifest.RootDirectoryPath, "CoiTelemetry.RealMod.dll");
        }

        void IMod.Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
            _logger.Info("Initializing");
            _context = new ModContext(_logger, _manifest.RootDirectoryPath, _manifest.RootDirectoryPath, Path.Combine(_manifest.RootDirectoryPath, "data"), resolver);
            _resolver = resolver;
            _events = resolver.Resolve<ISimLoopEvents>();
            _events.Sync.AddNonSaveable(this, UpdateOnSimulationTick);

            Reload();
        }

        public override void Dispose()
        {
            _logger.Info("Disposing");
            
            _current?.Stop();
            _current?.Dispose();
            _current = null;
        }

        private void UpdateOnSimulationTick()
        {
            if (File.Exists(_path))
            {
                var write = File.GetLastWriteTimeUtc(_path);
                if (write > _lastWriteUtc)
                {
                    Reload();
                }
            }

            try
            {
                _current?.OnSimulationTick();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to tick plugin: {e}");
            }
        }

        private void Reload()
        {
            try
            {
                _logger.Info("Reloading...");
                _current?.Stop();
                _current?.Dispose();
                _current = null;

                var dllBytes = File.ReadAllBytes(_path);
                var pdbPath = Path.ChangeExtension(_path, ".pdb");
                Assembly asm = File.Exists(pdbPath)
                    ? Assembly.Load(dllBytes, File.ReadAllBytes(pdbPath))
                    : Assembly.Load(dllBytes);

                var types = string.Join(", ", asm.GetTypes().Select(t => t.FullName));
                _logger.Info($"Loaded assembly: {asm.FullName}");
                var pluginType = asm.GetTypes().FirstOrDefault(t =>
                    typeof(IReloadableMod).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null );
                if (pluginType == null)
                {
                    throw new InvalidOperationException("No IReloadableMod found in assembly");
                }

                _current = (IReloadableMod)Activator.CreateInstance(pluginType);
                _current.Start(_context);

                _lastWriteUtc = File.GetLastWriteTimeUtc(_path);
                _logger.Info("Reloaded");
            }
            catch (Exception ex)
            {
                _logger.Info($"Failed to reload: {ex}");
            }
        }
        public override void RegisterPrototypes(ProtoRegistrator registrator)
        {
        }
    }
}