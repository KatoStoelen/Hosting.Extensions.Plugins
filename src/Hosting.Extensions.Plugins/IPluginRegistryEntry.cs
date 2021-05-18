using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hosting.Extensions.Plugins
{
    /// <summary>
    /// Represents an entry in a plugin registry.
    /// </summary>
    public interface IPluginRegistryEntry
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// Copies the plugin's content to the specified directory.
        /// </summary>
        /// <param name="directory">The target directory of the copy.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// A <see cref="Task"/> repserenting a potentially asynchronous
        /// copy operation.
        /// </returns>
        Task CopyTo(DirectoryInfo directory, CancellationToken cancellationToken);
    }
}