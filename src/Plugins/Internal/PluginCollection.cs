using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hosting.Extensions.Plugins.Internal.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginCollection : IPlugins, IDisposable
    {
        private readonly ManualResetEventSlim _initialLoadEvent = new();
        private readonly ReaderWriterLockSlim _readerWriterLock = new();
        private readonly HashSet<Plugin> _plugins = new();

        public IReadOnlyCollection<TPluginContract> GetImplementationsOf<TPluginContract>()
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

        public TPluginContract GetImplementation<TPluginContract>(string typeAssemblyQualifiedName)
        {
            _initialLoadEvent.Wait();
            _readerWriterLock.EnterReadLock();

            try
            {
                var (typeName, assemblyName) = typeAssemblyQualifiedName
                    .SplitTypeAndAssemblyName();

                var plugin = _plugins
                    .SingleOrDefault(plugin =>
                        plugin.AssemblyName.FullName
                            .Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

                if (plugin == null)
                {
                    throw new ArgumentException(
                        $"Could not find plugin assembly: {assemblyName}");
                }

                var contractImplementationType = plugin.GetType(typeName);
                if (contractImplementationType == null)
                {
                    throw new ArgumentException(
                        $"Type '{typeName}' not found in assembly: {assemblyName}");
                }

                return (TPluginContract)plugin.ServiceProvider
                    .GetRequiredService(contractImplementationType);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public IReadOnlyCollection<Type> GetTypesImplementing<TPluginContract>()
        {
            _initialLoadEvent.Wait();
            _readerWriterLock.EnterReadLock();

            try
            {
                return _plugins
                    .SelectMany(plugin => plugin.GetTypesImplementing<TPluginContract>())
                    .ToList()
                    .AsReadOnly();
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public bool IsNewerPluginVersion(string pluginName, SemanticVersion version)
        {
            _readerWriterLock.EnterReadLock();

            try
            {
                var existingPlugin = _plugins.SingleOrDefault(
                    plugin =>
                        pluginName.Equals(
                            plugin.AssemblyName.Name, StringComparison.OrdinalIgnoreCase));

                if (existingPlugin == null)
                {
                    return true;
                }

                return version > existingPlugin.Version;
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