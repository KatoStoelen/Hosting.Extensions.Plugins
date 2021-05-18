using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginsBuilder : IPluginsBuilder
    {
        private PluginsBuilder(IServiceCollection services, HostBuilderContext hostContext)
        {
            Services = services;
            HostContext = hostContext;
        }

        public IServiceCollection Services { get; }
        public HostBuilderContext HostContext { get; }

        public IPluginsBuilder WithRootDirectory(string rootDirectory)
        {
            Services.Configure<PluginsSettings>(
                settings => settings.RootDirectory = rootDirectory);

            return this;
        }

        public IPluginsBuilder MonitorRootDirectory()
        {
            Services.Configure<PluginsSettings>(
                settings => settings.MonitorRootDirectory = true);

            return this;
        }

        public IPluginsBuilder AddContractAssembly(Assembly assembly)
        {
            _ = AddSharedAssembly(assembly);

            var contractTypes = assembly
                .GetTypes()
                .Where(type => type.IsInterface || type.IsClass);

            Services.Configure<PluginsSettings>(settings =>
            {
                foreach (var contractType in contractTypes)
                {
                    settings.ContractTypes.Add(contractType);
                }
            });

            return this;
        }

        public IPluginsBuilder AddSharedAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Services.Configure<PluginsSettings>(
                settings => settings.SharedAssemblies.Add(assembly));

            return this;
        }

        public IPluginsBuilder CustomizePluginHostEnvironment(Action<IHostEnvironment> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            Services.Configure<PluginsSettings>(
                settings => settings.CustomHostEnvironmentConfigurer = configure);

            return this;
        }

        public IPluginsBuilder WithPluginServices(Action<IServiceCollection> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            Services.Configure<PluginsSettings>(
                settings => settings.CustomPluginServicesConfigurer = configure);

            var tempServices = new ServiceCollection();

            configure(tempServices);

            foreach (var descriptor in tempServices)
            {
                _ = AddSharedAssembly(descriptor.ServiceType.Assembly);
            }

            return this;
        }

        public IPluginsBuilder WithCustomPluginAssemblyLoader<TPluginAssemblyLoader>()
            where TPluginAssemblyLoader : class, IPluginAssemblyLoader
        {
            Services.Replace(
                ServiceDescriptor.Singleton<IPluginAssemblyLoader, TPluginAssemblyLoader>());

            return this;
        }

        public IPluginsBuilder WithRegistry<TPluginRegistry>(bool monitorForChanges = true)
            where TPluginRegistry : class, IPluginRegistry
        {
            Services.Replace(ServiceDescriptor.Singleton<IPluginRegistry, TPluginRegistry>());

            Services.Configure<PluginsSettings>(
                settings => settings.MonitorRegistry = monitorForChanges);

            return this;
        }

        public static PluginsBuilder CreateDefaultBuilder(
            IServiceCollection services, HostBuilderContext hostBuilderContext)
        {
            services
                .Configure<PluginsSettings>(settings =>
                {
                    settings.ApplicationName = hostBuilderContext.HostingEnvironment.ApplicationName;
                    settings.EnvironmentName = hostBuilderContext.HostingEnvironment.EnvironmentName;
                })
                .AddSingleton<PluginsRootDirectory>()
                .AddSingleton<PluginCollection>()
                .AddSingleton<IPlugins>(provider => provider.GetRequiredService<PluginCollection>())
                .AddSingleton(typeof(IPlugins<>), typeof(ImplementationResolver<>))
                .AddSingleton<IPluginAssemblyLoader, DefaultPluginAssemblyLoader>()
                .AddSingleton<IPluginRegistry, NoOpPluginRegistry>()
                .AddHostedService<PluginsHostedService>();

            return new PluginsBuilder(services, hostBuilderContext);
        }
    }
}