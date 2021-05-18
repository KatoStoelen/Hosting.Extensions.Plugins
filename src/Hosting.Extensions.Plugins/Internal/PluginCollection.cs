using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginCollection : IDisposable
    {
        private readonly ManualResetEventSlim _initialLoadEvent = new ManualResetEventSlim();
        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim();
        private readonly HashSet<Plugin> _plugins = new HashSet<Plugin>();

        public IReadOnlyCollection<TPluginContract> Get<TPluginContract>()
        {
            _initialLoadEvent.Wait();
            _readerWriterLock.EnterReadLock();

            try
            {
                return _plugins
                    .SelectMany(
                        plugin =>
                            plugin.ServiceProvider.GetServices<TPluginContract>())
                    .ToList()
                    .AsReadOnly();
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public void AddPlugin(Plugin plugin)
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
                _ = _plugins.Add(plugin);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public void RemovePlugin(DirectoryInfo pluginDirectory)
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
                var pluginToRemove = _plugins
                    .SingleOrDefault(plugin => plugin.ResidesIn(pluginDirectory));

                if (pluginToRemove != null)
                {
                    _ = _plugins.Remove(pluginToRemove);

                    pluginToRemove.Dispose();
                }
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public bool ContainsPluginResidingIn(DirectoryInfo pluginDirectory)
        {
            _readerWriterLock.EnterReadLock();

            try
            {
                return _plugins.Any(plugin => plugin.ResidesIn(pluginDirectory));
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public void SetInitialLoadingCompleted()
        {
            _initialLoadEvent.Set();
        }

        public void Dispose()
        {
            _readerWriterLock.Dispose();

            foreach (var plugin in _plugins)
            {
                plugin.Dispose();
            }

            _plugins.Clear();
        }
    }
}