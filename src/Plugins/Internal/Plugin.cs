using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using McMaster.NETCore.Plugins;
using NuGet.Versioning;

namespace Hosting.Extensions.Plugins.Internal
{
    internal sealed class Plugin : IDisposable, IEquatable<Plugin>
    {
        private readonly ManualResetEventSlim _loadedEvent;
        private readonly IPluginAssemblyLoader _pluginAssemblyLoader;
        private readonly DirectoryInfo _pluginDirectory;
        private readonly FileInfo _mainAssemblyFile;
        private readonly Lazy<AssemblyName> _assemblyName;
        private readonly Lazy<SemanticVersion> _version;
        private PluginLoader? _loader;
        private Assembly? _mainAssembly;
        private IServiceProvider? _serviceProvider;
        private bool _disposed;

        internal Plugin(
            DirectoryInfo pluginDirectory,
            PluginsSettings settings,
            IPluginAssemblyLoader pluginAssemblyLoader)
        {
            _loadedEvent = new ManualResetEventSlim();
            _pluginAssemblyLoader = pluginAssemblyLoader;
            _pluginDirectory = pluginDirectory;

            _mainAssemblyFile = settings.GetMainPluginAssembly(pluginDirectory);
            _assemblyName = new(
                () => AssemblyName.GetAssemblyName(_mainAssemblyFile.FullName),
                LazyThreadSafetyMode.ExecutionAndPublication);
            _version = new(GetPluginVersion, LazyThreadSafetyMode.ExecutionAndPublication);

            _loader = settings.GetPluginLoader(_mainAssemblyFile);
            _loader.Reloaded += OnReloaded;
        }

        public IServiceProvider ServiceProvider
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Plugin));
                }

                _loadedEvent.Wait();

                return _serviceProvider!;
            }
        }

        public AssemblyName AssemblyName => _assemblyName.Value;
        public SemanticVersion Version => _version.Value;

        public IEnumerable<Type> GetTypesImplementing<TPluginContract>()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Plugin));
            }

            _loadedEvent.Wait();

            return _mainAssembly!.GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.IsAssignableTo(typeof(TPluginContract)));
        }

        public Type? GetType(string typeFullName)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Plugin));
            }

            _loadedEvent.Wait();

            return _mainAssembly!.GetType(typeFullName);
        }

        public void Load()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Plugin));
            }

            _loadedEvent.Reset();

            if (_serviceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }

            _mainAssembly = _loader!.LoadDefaultAssembly();
            _serviceProvider = _pluginAssemblyLoader.Load(_mainAssembly, _pluginDirectory);

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
            _mainAssembly = null;

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

        private SemanticVersion GetPluginVersion()
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(_mainAssemblyFile.FullName);

            if (!string.IsNullOrWhiteSpace(versionInfo.ProductVersion))
            {
                return SemanticVersion.Parse(versionInfo.ProductVersion);
            }

            if (!string.IsNullOrWhiteSpace(versionInfo.FileVersion))
            {
                return SemanticVersion.Parse(versionInfo.FileVersion);
            }

            var assemblyVersion = AssemblyName.Version;

            if (assemblyVersion != null)
            {
                return new SemanticVersion(
                    assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
            }

            throw new InvalidOperationException(
                $"Unable to determine version of plugin: {_mainAssemblyFile}");
        }
    }
}