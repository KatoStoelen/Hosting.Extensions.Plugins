using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

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
        /// The version of the plugin.
        /// </summary>
        SemanticVersion Version { get; }

        /// <summary>
        /// Copies the plugin's content to the specified directory.
        /// </summary>
        /// <param name="directory">The target directory of the copy.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A <see cref="Task"/> repserenting the asynchronous copy operation.
        /// </returns>
        Task CopyToAsync(DirectoryInfo directory, CancellationToken cancellationToken);
    }
}