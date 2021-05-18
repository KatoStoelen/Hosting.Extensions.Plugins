using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginsBuilder : IPluginsBuilder
    {
        private readonly IServiceCollection _services;

        private PluginsBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public IPluginsBuilder WithRootDirectory(string rootDirectory)
        {
            _services.Configure<PluginsSettings>(
                settings => settings.RootDirectory = rootDirectory);

            return this;
        }

        public IPluginsBuilder MonitorRootDirectory()
        {
            _services.Configure<PluginsSettings>(
                settings => settings.MonitorRootDirectory = true);

            return this;
        }

        public IPluginsBuilder WithPluginContractAssembly(Assembly assembly)
        {
            _services.Configure<PluginsSettings>(
                settings => settings.SharedAssemblies.Add(assembly));

            return this;
        }

        public IPluginsBuilder CustomizePluginHostEnvironment(Action<IHostEnvironment> configure)
        {
            _services.Configure<PluginsSettings>(
                settings => settings.CustomHostEnvironmentConfigurer = configure);

            return this;
        }

        public IPluginsBuilder WithPluginServices(params ServiceDescriptor[] descriptors)
        {
            _services.Configure<PluginsSettings>(
                settings => settings.CustomPluginServices.AddRange(descriptors));

            foreach (var descriptor in descriptors)
            {
                _ = WithPluginContractAssembly(descriptor.ServiceType.Assembly);
            }

            return this;
        }

        public IPluginsBuilder WithCustomPluginAssemblyLoader<TPluginAssemblyLoader>()
            where TPluginAssemblyLoader : class, IPluginAssemblyLoader
        {
            _services.Replace(
                ServiceDescriptor.Singleton<IPluginAssemblyLoader, TPluginAssemblyLoader>());

            return this;
        }

        public IPluginsBuilder WithRegistry<TPluginRegistry>()
            where TPluginRegistry : class, IPluginRegistry
        {
            _services.Replace(ServiceDescriptor.Singleton<IPluginRegistry, TPluginRegistry>());

            return this;
        }

        public static PluginsBuilder CreateDefaultBuilder(
            IServiceCollection services, HostBuilderContext hostBuilderContext)
        {
            services
                // .Configure<PluginsSettings>("Plugins", hostBuilderContext.Configuration)
                .Configure<PluginsSettings>(settings =>
                {
                    settings.ApplicationName = hostBuilderContext.HostingEnvironment.ApplicationName;
                    settings.EnvironmentName = hostBuilderContext.HostingEnvironment.EnvironmentName;
                })
                .AddSingleton<PluginsRootDirectory>()
                .AddSingleton<PluginCollection>()
                .AddSingleton<IPluginAssemblyLoader, DefaultPluginAssemblyLoader>()
                .AddSingleton<IPluginRegistry, NoOpPluginRegistry>()
                .AddHostedService<PluginsHostedService>();

            return new PluginsBuilder(services);
        }
    }
}