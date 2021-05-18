using System;
using System.IO;
using System.Reflection;

namespace Hosting.Extensions.Plugins
{
    /// <summary>
    /// Responsible for loading a plugin's main assembly and building an
    /// <see cref="IServiceProvider"/> from witch plugin implementations
    /// can be resolved.
    /// </summary>
    public interface IPluginAssemblyLoader
    {
        /// <summary>
        /// Loads the specified plugin assembly into an
        /// <see cref="IServiceProvider"/> from witch plugin
        /// implementations can be resolved.
        /// </summary>
        /// <param name="mainPluginAssembly">The plugin's main assembly.</param>
        /// <param name="pluginDirectory">The directory of the plugin.</param>
        /// <returns>
        /// An <see cref="IServiceProvider"/> from witch plugin
        /// implementations can be resolved.
        /// </returns>
        IServiceProvider Load(Assembly mainPluginAssembly, DirectoryInfo pluginDirectory);
    }
}