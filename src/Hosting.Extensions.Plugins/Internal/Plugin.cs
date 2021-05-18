using System;
using System.IO;
using System.Threading;
using McMaster.NETCore.Plugins;

namespace Hosting.Extensions.Plugins.Internal
{
    internal sealed class Plugin : IDisposable, IEquatable<Plugin>
    {
        private readonly ManualResetEventSlim _loadedEvent;
        private readonly IPluginAssemblyLoader _pluginAssemblyLoader;
        private readonly DirectoryInfo _pluginDirectory;
        private IServiceProvider? _serviceProvider;
        private PluginLoader? _loader;
        private bool _disposed;

        internal Plugin(
            DirectoryInfo pluginDirectory,
            PluginsSettings settings,
            IPluginAssemblyLoader pluginAssemblyLoader)
        {
            _loadedEvent = new ManualResetEventSlim();
            _pluginAssemblyLoader = pluginAssemblyLoader;
            _pluginDirectory = pluginDirectory;

            var mainAssemblyFile = settings.GetMainPluginAssembly(pluginDirectory);

            _loader = settings.GetPluginLoader(mainAssemblyFile);
            _loader.Reloaded += OnReloaded;
        }

        public IServiceProvider ServiceProvider
        {
            get
            {
                _loadedEvent.Wait();

                return _serviceProvider!;
            }
        }

        public void Load()
        {
            _loadedEvent.Reset();

            if (_serviceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }

            var mainAssembly = _loader!.LoadDefaultAssembly();
            _serviceProvider = _pluginAssemblyLoader.Load(mainAssembly, _pluginDirectory);

            _loadedEvent.Set();
        }

        private void OnReloaded(object sender, PluginReloadedEventArgs eventArgs)
        {
            Load();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _loadedEvent.Dispose();

            if (_serviceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }

            _serviceProvider = null;

            var loaderWeakReference = new WeakReference(_loader, trackResurrection: true);

            _loader!.Reloaded -= OnReloaded;
            _loader!.Dispose();
            _loader = null;

            for (var i = 0; loaderWeakReference.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            _disposed = true;
        }

        public override bool Equals(object? obj)
        {
            return obj is Plugin plugin && Equals(plugin);
        }

        public override int GetHashCode()
        {
            return _pluginDirectory.FullName.ToLowerInvariant().GetHashCode();
        }

        public bool Equals(Plugin? other)
        {
            return other != null && ResidesIn(other._pluginDirectory);
        }

        public bool ResidesIn(DirectoryInfo otherPluginDirectory)
        {
            return
                _pluginDirectory.FullName.Equals(
                    otherPluginDirectory.FullName, StringComparison.OrdinalIgnoreCase);
        }
    }
}