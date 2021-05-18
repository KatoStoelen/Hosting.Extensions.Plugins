using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hosting.Extensions.Plugins
{
    /// <summary>
    /// A builder for configuring plugin settings.
    /// </summary>
    public interface IPluginsBuilder
    {
        /// <summary>
        /// Sets the directory where plugins can be found.
        /// </summary>
        /// <param name="rootDirectory">The directory where plugins can be found.</param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder WithRootDirectory(string rootDirectory);

        /// <summary>
        /// Enables monitoring of the plugins root directory. This makes it
        /// possible to add or remove plugins while the host is running.
        /// </summary>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder MonitorRootDirectory();

        /// <summary>
        /// Specifies that the specified assembly should be shared between
        /// the host and the plugins.
        /// </summary>
        /// <param name="assembly">The assembly to be shared between the host and the plugins.</param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder WithPluginContractAssembly(Assembly assembly);

        /// <summary>
        /// Customizes the <see cref="IHostEnvironment"/> passed to plugins.
        /// </summary>
        /// <remarks>
        /// By default, <see cref="IHostEnvironment.ApplicationName"/> and
        /// <see cref="IHostEnvironment.EnvironmentName"/> will be inherited from
        /// the host.
        /// <para>
        /// <see cref="IHostEnvironment.ContentRootPath"/> and
        /// <see cref="IHostEnvironment.ContentRootFileProvider"/> point to the
        /// plugin's directory and can be used to load configuration files etc.
        /// </para>
        /// </remarks>
        /// <param name="configure">A delegate configuring the plugins' <see cref="IHostEnvironment"/>.</param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder CustomizePluginHostEnvironment(Action<IHostEnvironment> configure);

        /// <summary>
        /// Configures additional services resolvable by the plugins.
        /// </summary>
        /// <param name="descriptors">Descriptors of the additional services.</param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder WithPluginServices(params ServiceDescriptor[] descriptors);

        /// <summary>
        /// Use a custom <see cref="IPluginAssemblyLoader"/>.
        /// </summary>
        /// <typeparam name="TPluginAssemblyLoader">An <see cref="IPluginAssemblyLoader"/> implementation.</typeparam>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder WithCustomPluginAssemblyLoader<TPluginAssemblyLoader>()
            where TPluginAssemblyLoader : class, IPluginAssemblyLoader;

        /// <summary>
        /// Load plugins from the specified registry.
        /// </summary>
        /// <remarks>
        /// By default, no plugin registry is used, and only the plugins that
        /// are present in the plugins root directory when the host starts will
        /// be loaded.
        /// </remarks>
        /// <typeparam name="TPluginRegistry">An <see cref="IPluginRegistry"/> implementation.</typeparam>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder WithRegistry<TPluginRegistry>() where TPluginRegistry : class, IPluginRegistry;
    }
}