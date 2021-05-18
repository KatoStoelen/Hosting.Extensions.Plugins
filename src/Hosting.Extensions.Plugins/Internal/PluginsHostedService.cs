using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginsHostedService : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly PluginsRootDirectory _pluginsRootDirectory;
        private readonly PluginCollection _pluginCollection;
        private readonly IPluginAssemblyLoader _pluginAssemblyLoader;
        private readonly IPluginRegistry _pluginRegistry;
        private readonly PluginsSettings _settings;

        private CancellationTokenSource? _combinedCts;
        private Task? _initialLoadTask;
        private IDisposable? _pluginRegistryListener;
        private bool _disposed;

        public PluginsHostedService(
            PluginsRootDirectory pluginsRootDirectory,
            PluginCollection pluginCollection,
            IPluginAssemblyLoader pluginAssemblyLoader,
            IPluginRegistry pluginRegistry,
            IOptions<PluginsSettings> settings)
        {
            _cts = new CancellationTokenSource();
            _pluginsRootDirectory = pluginsRootDirectory;
            _pluginCollection = pluginCollection;
            _pluginAssemblyLoader = pluginAssemblyLoader;
            _pluginRegistry = pluginRegistry;
            _settings = settings.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _combinedCts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cts.Token);

            _initialLoadTask = LoadInitialPlugins(_combinedCts.Token);

            if (_settings.MonitorRootDirectory)
            {
                _pluginsRootDirectory.Changed += OnPluginsRootDirectoryChanged;
                _pluginsRootDirectory.StartMonitoring();
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();

            if (_settings.MonitorRootDirectory)
            {
                _pluginsRootDirectory.StopMonitoring();
                _pluginsRootDirectory.Changed -= OnPluginsRootDirectoryChanged;
            }

            _pluginRegistryListener?.Dispose();

            try
            {
                await _initialLoadTask!.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        private async Task LoadInitialPlugins(CancellationToken cancellationToken)
        {
            await foreach (var pluginEntry in _pluginRegistry.GetEntries())
            {
                _ = await CopyRegistryEntryAsync(pluginEntry, cancellationToken);
            }

            _ = Parallel.ForEach(
                _pluginsRootDirectory.PluginDirectories,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken
                },
                pluginDirectoryPath => AddPlugin(pluginDirectoryPath));

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (_pluginRegistry.ShouldMonitorForChanges)
            {
                _pluginRegistryListener = _pluginRegistry
                    .StartMonitoring(OnPluginRegistryChanged);
            }

            _pluginCollection.SetInitialLoadingCompleted();
        }

        private void OnPluginsRootDirectoryChanged(object sender, PluginsRootDirectoryEventArgs args)
        {
            switch (args.Change)
            {
                case PluginsRootDirectoryEventArgs.ChangeType.Added:
                    AddPlugin(args.FullPath);
                    break;
                case PluginsRootDirectoryEventArgs.ChangeType.Removed:
                    RemovePlugin(args.FullPath);
                    break;
                case PluginsRootDirectoryEventArgs.ChangeType.Renamed:
                    RemovePlugin(((PluginDirectoryRenamedEventArgs)args).OldFullPath);
                    AddPlugin(args.FullPath);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported plugins root directory change type: {args.Change}");
            }
        }

        private void OnPluginRegistryChanged(IPluginRegistryEntry pluginEntry)
        {
            _ = Task.Run(async () =>
            {
                var pluginDirectory = await CopyRegistryEntryAsync(pluginEntry, _combinedCts!.Token);

                if (!_pluginCollection.ContainsPluginResidingIn(pluginDirectory))
                {
                    AddPlugin(pluginDirectory.FullName);
                }
            }, _combinedCts!.Token);
        }

        private async Task<DirectoryInfo> CopyRegistryEntryAsync(
            IPluginRegistryEntry pluginEntry, CancellationToken cancellationToken)
        {
            var pluginDirectory = _pluginsRootDirectory
                .GetPluginDirectory(pluginEntry.PluginName);

            if (!pluginDirectory.Exists)
            {
                pluginDirectory.Create();
            }

            await pluginEntry.CopyTo(pluginDirectory, cancellationToken);

            return pluginDirectory;
        }

        private void AddPlugin(string pluginDirectoryPath)
        {
            var pluginDirectory = new DirectoryInfo(pluginDirectoryPath);
            var plugin = new Plugin(pluginDirectory, _settings, _pluginAssemblyLoader);

            plugin.Load();

            _pluginCollection.AddPlugin(plugin);
        }

        private void RemovePlugin(string pluginDirectoryPath)
        {
            var pluginDirectory = new DirectoryInfo(pluginDirectoryPath);

            _pluginCollection.RemovePlugin(pluginDirectory);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cts.Dispose();
            _combinedCts?.Dispose();
            _pluginRegistryListener?.Dispose();
            _pluginsRootDirectory.Dispose();
            _pluginCollection.Dispose();

            _disposed = true;
        }
    }
}