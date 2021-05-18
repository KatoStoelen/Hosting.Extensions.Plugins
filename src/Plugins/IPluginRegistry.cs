using System;
using System.Collections.Generic;
using System.Threading;

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
        /// Whether or not the registry supports monitoring of changes to
        /// plugins.
        /// </summary>
        bool CanMonitor { get; }

        /// <summary>
        /// Gets the latest version of all plugin entries in this registry.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <remarks>
        /// This method is only invoked during host start up to get the
        /// initial set of plugins. To load or reload plugins when the
        /// registry is updated, the registry must support monitoring
        /// of changes and registry monitoring must be enabled.
        /// </remarks>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing the plugin
        /// entries in this registry.
        /// </returns>
        IAsyncEnumerable<IPluginRegistryEntry> GetLatestVersionOfEntries(
            CancellationToken cancellationToken = default);

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
        IAsyncDisposable StartMonitoring(Action<IPluginRegistryEntry> onEntryChanged);
    }
}