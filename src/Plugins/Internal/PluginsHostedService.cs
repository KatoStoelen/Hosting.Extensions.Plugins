using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<PluginsHostedService> _logger;

        private CancellationTokenSource? _combinedCts;
        private Task? _initialLoadTask;
        private Stopwatch? _stopwatch;
        private IAsyncDisposable? _pluginRegistryListener;
        private bool _disposed;

        public PluginsHostedService(
            PluginsRootDirectory pluginsRootDirectory,
            PluginCollection pluginCollection,
            IPluginAssemblyLoader pluginAssemblyLoader,
            IPluginRegistry pluginRegistry,
            IOptions<PluginsSettings> settings,
            ILogger<PluginsHostedService> logger)
        {
            _cts = new CancellationTokenSource();
            _pluginsRootDirectory = pluginsRootDirectory;
            _pluginCollection = pluginCollection;
            _pluginAssemblyLoader = pluginAssemblyLoader;
            _pluginRegistry = pluginRegistry;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _combinedCts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cts.Token);

            _logger.LogInformation("Starting plugin services");

            _stopwatch = Stopwatch.StartNew();

            _pluginsRootDirectory.EnsureExists();

            _initialLoadTask = LoadInitialPlugins(_combinedCts.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();

            _logger.LogInformation("Stopping plugin services");

            if (_settings.MonitorRootDirectory)
            {
                _pluginsRootDirectory.StopMonitoring();
                _pluginsRootDirectory.Changed -= OnPluginsRootDirectoryChanged;
            }

            if (_pluginRegistryListener != null)
            {
                await _pluginRegistryListener.DisposeAsync().ConfigureAwait(false);
            }

            try
            {
                await _initialLoadTask!.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        private async Task LoadInitialPlugins(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Loading initial plugins");

            try
            {
                _logger.LogDebug("Loading plugins in root directory");

                await TaskExecutor
                    .Parallel(
                        _pluginsRootDirectory.PluginDirectories,
                        AddPlugin,
                        OnPluginError,
                        cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Copying latest plugin versions from registry");

                await TaskExecutor
                    .Parallel(
                        _pluginRegistry.GetLatestVersionOfEntries(cancellationToken),
                        pluginEntry => CopyRegistryEntryAsync(pluginEntry, cancellationToken),
                        OnRegistryEntryError,
                        cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug(
                    "Initial plugin loading complete ({Elapsed} ms)",
                    _stopwatch!.ElapsedMilliseconds);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (_settings.MonitorRootDirectory)
                {
                    _pluginsRootDirectory.Changed += OnPluginsRootDirectoryChanged;
                    _pluginsRootDirectory.StartMonitoring();
                }

                if (_pluginRegistry.CanMonitor && _settings.MonitorRegistry)
                {
                    _pluginRegistryListener = _pluginRegistry
                        .StartMonitoring(OnPluginRegistryChanged);
                }

                _pluginCollection.SetInitialLoadingCompleted();

                _logger.LogInformation(
                    "Plugin services started ({Elapsed} ms)",
                    _stopwatch.ElapsedMilliseconds);

                _stopwatch!.Stop();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start plugin services: {Message}", e.Message);
            }
        }

        private void OnPluginsRootDirectoryChanged(
            object sender, PluginsRootDirectoryEventArgs args)
        {
            _logger.LogDebug(
                "Root directory changed: {Directory} ({Change})", args.FullPath, args.Change);

            TaskExecutor.ExecuteAsync(
                () =>
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
                            throw new InvalidOperationException(
                                $"Unsupported plugins root directory change type: {args.Change}");
                    }
                },
                ex => OnPluginError(args.FullPath, ex),
                _combinedCts!.Token);
        }

        private void OnPluginRegistryChanged(IPluginRegistryEntry pluginEntry)
        {
            _logger.LogDebug(
                "Plugin registry changed: {Plugin} (v{Version})",
                pluginEntry.PluginName,
                pluginEntry.Version);

            TaskExecutor.Execute(
                () => CopyRegistryEntryAsync(pluginEntry, _combinedCts!.Token),
                exception => OnRegistryEntryError(pluginEntry, exception),
                _combinedCts!.Token);
        }

        private async Task CopyRegistryEntryAsync(
            IPluginRegistryEntry pluginEntry, CancellationToken cancellationToken)
        {
            if (!_pluginCollection.IsNewerPluginVersion(pluginEntry.PluginName, pluginEntry.Version))
            {
                _logger.LogDebug(
                    "Skipped copying registry entry '{Plugin} v{Version}'. " +
                    "Newer or equal version already installed",
                    pluginEntry.PluginName,
                    pluginEntry.Version.ToNormalizedString());

                return;
            }

            var pluginDirectory = _pluginsRootDirectory
                .GetPluginDirectory(pluginEntry.PluginName);

            if (!pluginDirectory.Exists)
            {
                pluginDirectory.Create();
            }

            _logger.LogDebug(
                "Copying {Plugin} from registry to {Directory}",
                pluginEntry.PluginName,
                pluginDirectory);

            await pluginEntry.CopyToAsync(pluginDirectory, cancellationToken)
                .ConfigureAwait(false);

            if (_pluginCollection.ContainsPluginResidingIn(pluginDirectory))
            {
                _logger.LogDebug(
                    "Plugin '{Plugin}' already loaded. Will reload automatically",
                    pluginEntry.PluginName);
            }
            else
            {
                _logger.LogDebug("Loading new plugin: {Plugin}", pluginEntry.PluginName);

                AddPlugin(pluginDirectory.FullName);
            }
        }

        private void OnRegistryEntryError(IPluginRegistryEntry entry, Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load plugin '{Plugin}' from registry",
                entry.PluginName);
        }

        private void OnPluginError(string pluginDirectoryPath, Exception exception)
        {
            _logger.LogError(
                exception,
                "Exception when processing plugin in directory: {Path}",
                pluginDirectoryPath);
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
            _pluginsRootDirectory.Dispose();
            _pluginCollection.Dispose();

            _disposed = true;
        }
    }
}