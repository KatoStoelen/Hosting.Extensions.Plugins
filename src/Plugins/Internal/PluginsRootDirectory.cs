using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hosting.Extensions.Plugins.Internal
{
    internal delegate void PluginsRootDirectoryChangeEventHandler(
        object sender, PluginsRootDirectoryEventArgs args);

    internal class PluginsRootDirectory : IDisposable
    {
        private readonly string _rootDirectoryPath;
        private readonly ILogger<PluginsRootDirectory> _logger;
        private FileSystemWatcher? _fileSystemWatcher;
        private bool _disposed;

        public event PluginsRootDirectoryChangeEventHandler? Changed;

        public PluginsRootDirectory(
            IOptions<PluginsSettings> settings, ILogger<PluginsRootDirectory> logger)
        {
            _rootDirectoryPath = Environment.ExpandEnvironmentVariables(
                settings.Value.RootDirectory);
            _logger = logger;
        }

        public IEnumerable<string> PluginDirectories =>
            Directory.EnumerateDirectories(
                _rootDirectoryPath, "*", SearchOption.TopDirectoryOnly);

        public void EnsureExists()
        {
            if (!Directory.Exists(_rootDirectoryPath))
            {
                _ = Directory.CreateDirectory(_rootDirectoryPath);
            }
        }

        public DirectoryInfo GetPluginDirectory(string pluginName)
        {
            return new DirectoryInfo(Path.Combine(_rootDirectoryPath, pluginName));
        }

        public void StartMonitoring()
        {
            _fileSystemWatcher = new FileSystemWatcher(_rootDirectoryPath)
            {
                NotifyFilter = NotifyFilters.DirectoryName
            };

            _fileSystemWatcher.Created += OnDirectoryCreated;
            _fileSystemWatcher.Deleted += OnDirectoryDeleted;
            _fileSystemWatcher.Renamed += OnDirectoryRenamed;
            _fileSystemWatcher.EnableRaisingEvents = true;

            _logger.LogDebug(
                "Started monitoring of plugin root directory: {Directory}",
                _rootDirectoryPath);
        }

        public void StopMonitoring()
        {
            if (_fileSystemWatcher == null)
            {
                return;
            }

            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Created -= OnDirectoryCreated;
            _fileSystemWatcher.Deleted -= OnDirectoryDeleted;
            _fileSystemWatcher.Renamed -= OnDirectoryRenamed;

            _logger.LogDebug(
                "Stopped monitoring of plugin root directory: {Directory}",
                _rootDirectoryPath);
        }

        private void OnDirectoryCreated(object sender, FileSystemEventArgs args)
        {
            Changed?.Invoke(
                this,
                new PluginsRootDirectoryEventArgs(
                    args.FullPath,
                    PluginsRootDirectoryEventArgs.ChangeType.Added));
        }

        private void OnDirectoryDeleted(object sender, FileSystemEventArgs args)
        {
            Changed?.Invoke(
                this,
                new PluginsRootDirectoryEventArgs(
                    args.FullPath,
                    PluginsRootDirectoryEventArgs.ChangeType.Removed));
        }

        private void OnDirectoryRenamed(object sender, RenamedEventArgs args)
        {
            Changed?.Invoke(
                this,
                new PluginDirectoryRenamedEventArgs(args.FullPath, args.OldFullPath));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _fileSystemWatcher?.Dispose();

            _disposed = true;
        }
    }

    internal class PluginsRootDirectoryEventArgs
    {
        public PluginsRootDirectoryEventArgs(string fullPath, ChangeType change)
        {
            FullPath = fullPath;
            Change = change;
        }

        public string FullPath { get; }
        public ChangeType Change { get; }

        public enum ChangeType
        {
            Added,
            Removed,
            Renamed
        }
    }

    internal class PluginDirectoryRenamedEventArgs : PluginsRootDirectoryEventArgs
    {
        public PluginDirectoryRenamedEventArgs(string fullPath, string oldFullPath)
            : base(fullPath, ChangeType.Renamed)
        {
            OldFullPath = oldFullPath;
        }

        public string OldFullPath { get; }
    }
}