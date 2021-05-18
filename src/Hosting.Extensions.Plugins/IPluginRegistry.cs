using System;
using System.Collections.Generic;

namespace Hosting.Extensions.Plugins
{
    /// <summary>
    /// Represents a registry of plugins relevant for this host.
    /// 
    /// <para>
    /// The plugins in this registry will be copied (or downloaded) into
    /// the root directory for plugins set via
    /// <see cref="IPluginsBuilder.WithRootDirectory(string)"/> and loaded
    /// into the host from there.
    /// </para>
    /// </summary>
    public interface IPluginRegistry
    {
        /// <summary>
        /// Whether or not the registry should be monitored for changes.
        /// </summary>
        bool ShouldMonitorForChanges { get; }

        /// <summary>
        /// Gets the plugin entries in this registry.
        /// </summary>
        /// <remarks>
        /// This method is only invoked during host start up to get the
        /// initial set of plugins. To load or reload plugins when the
        /// registry is updated, <see cref="ShouldMonitorForChanges"/>
        /// must return <see langword="true"/> and
        /// <see cref="StartMonitoring(Action{IPluginRegistryEntry})"/>
        /// must be supported.
        /// </remarks>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing the plugin
        /// entries in this registry.
        /// </returns>
        IAsyncEnumerable<IPluginRegistryEntry> GetEntries();

        /// <summary>
        /// Starts monitoring the registry for changes.
        /// </summary>
        /// <param name="onEntryChanged">
        /// The delegate to invoke when changes occur.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> that stops the monitoring when
        /// disposed.
        /// </returns>
        IDisposable StartMonitoring(Action<IPluginRegistryEntry> onEntryChanged);
    }
}