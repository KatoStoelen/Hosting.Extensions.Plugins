using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;

namespace Hosting.Extensions.Plugins.Internal
{
    internal delegate void PluginsRootDirectoryChangeEventHandler(
        object sender, PluginsRootDirectoryEventArgs args);

    internal class PluginsRootDirectory : IDisposable
    {
        private readonly PluginsSettings _settings;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private bool _disposed;

        public event PluginsRootDirectoryChangeEventHandler? Changed;

        public PluginsRootDirectory(IOptions<PluginsSettings> settings)
        {
            _settings = settings.Value;
            _fileSystemWatcher = new FileSystemWatcher(_settings.RootDirectory)
            {
                NotifyFilter = NotifyFilters.DirectoryName
            };
        }

        public IEnumerable<string> PluginDirectories =>
            Directory.EnumerateDirectories(
                _settings.RootDirectory, "*", SearchOption.TopDirectoryOnly);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _fileSystemWatcher.Dispose();

            _disposed = true;
        }

        public DirectoryInfo GetPluginDirectory(string pluginName)
        {
            return new DirectoryInfo(Path.Combine(_settings.RootDirectory, pluginName));
        }

        public void StartMonitoring()
        {
            _fileSystemWatcher.Created += OnDirectoryCreated;
            _fileSystemWatcher.Deleted += OnDirectoryDeleted;
            _fileSystemWatcher.Renamed += OnDirectoryRenamed;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void StopMonitoring()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Created -= OnDirectoryCreated;
            _fileSystemWatcher.Deleted -= OnDirectoryDeleted;
            _fileSystemWatcher.Renamed -= OnDirectoryRenamed;
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