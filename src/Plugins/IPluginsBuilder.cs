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
        /// The <see cref="IServiceCollection"/> instance of the <see cref="IHostBuilder"/>.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// The <see cref="HostBuilderContext"/> of the <see cref="IHostBuilder"/>.
        /// </summary>
        HostBuilderContext HostContext { get; }

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
        /// Adds the specified assembly as a shared assembly
        /// (<see cref="AddSharedAssembly(Assembly)"/>) and makes sure contract
        /// implementations are correctly registered in the plugins' DI container.
        /// </summary>
        /// <remarks>
        /// For plugins using dependency injection, specifying the contract assemblies
        /// using this method makes sure contract implementations are also registered
        /// as the contract types in the DI container. This makes it easier for plugins
        /// to register their implementations.
        /// </remarks>
        /// <param name="assembly">An assembly containing plugin contract types.</param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder AddContractAssembly(Assembly assembly);

        /// <summary>
        /// Adds an assembly to be shared between the host and the plugins. This
        /// method can be called multiple times.
        /// </summary>
        /// <remarks>
        /// Shared assemblies should not be a part of plugins' artifacts, as they
        /// are loaded into the plugins from the host.
        /// 
        /// <para>
        /// More info:
        /// </para>
        /// <para>
        /// https://github.com/natemcmaster/DotNetCorePlugins/blob/main/docs/what-are-shared-types.md
        /// </para>
        /// </remarks>
        /// <param name="assembly">The assembly to be shared between the host and the plugins.</param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder AddSharedAssembly(Assembly assembly);

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
        /// <remarks>
        /// The assembly of each <see cref="ServiceDescriptor.ServiceType"/> added
        /// to the <see cref="IServiceCollection"/> automatically becomes a shared
        /// assembly.
        /// 
        /// <para>
        /// For more info about shared assemblies, see <seealso cref="AddSharedAssembly(Assembly)"/>.
        /// </para>
        /// </remarks>
        /// <param name="configure">A delegate configuring the additional services.</param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder WithPluginServices(Action<IServiceCollection> configure);

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
        /// are present in the configured root directory when the host starts
        /// will be loaded.
        /// </remarks>
        /// <param name="monitorForChanges">
        /// Whether or not to monitor the registry for changes and
        /// automatically load plugins when updates occur.
        /// 
        /// <para>
        /// Defaults to <see langword="true"/>, but only has an effect if the
        /// registry supports monitoring.
        /// </para>
        /// </param>
        /// <typeparam name="TPluginRegistry">An <see cref="IPluginRegistry"/> implementation.</typeparam>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        IPluginsBuilder WithRegistry<TPluginRegistry>(bool monitorForChanges = true)
            where TPluginRegistry : class, IPluginRegistry;
    }
}