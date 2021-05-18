using System;
using System.Linq;
using System.Reflection;
using Hosting.Extensions.Plugins.Internal.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginAssemblyEntryPoint
    {
        private readonly Lazy<MethodInfo?> _configureServicesMethod;

        public PluginAssemblyEntryPoint(Assembly pluginAssembly)
        {
            _configureServicesMethod = new Lazy<MethodInfo?>(
                () => GetConfigureServicesMethod(pluginAssembly));
        }

        public bool Exists => _configureServicesMethod.Value != null;

        private static MethodInfo? GetConfigureServicesMethod(Assembly pluginAssembly)
        {
            var entryPoints = pluginAssembly
                .GetTypes()
                .Where(type => type.Name.Equals("Startup", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!entryPoints.Any())
            {
                return null;
            }

            if (entryPoints.Count > 1)
            {
                throw new InvalidOperationException(
                    $"More than one entry point found in plugin assembly {pluginAssembly.GetName().Name}");
            }

            return entryPoints.Single().GetMethod("ConfigureServices");
        }

        public ServiceCollection ConfigureServices(IServiceCollection servicesResolvableByPlugins)
        {
            if (!Exists)
            {
                throw new InvalidOperationException($"Plugin assembly does not have an entry point");
            }

            var configureServicesMethod = _configureServicesMethod.Value!;
            var entryPointType = configureServicesMethod.DeclaringType!;

            var servicesRegisteredByPlugin = new ServiceCollection();

            var temporaryServiceCollection = new ServiceCollection()
                .Add(servicesResolvableByPlugins)
                .AddSingleton<IServiceCollection>(servicesRegisteredByPlugin)
                .AddSingleton(entryPointType);

            using var serviceProvider = temporaryServiceCollection.BuildServiceProvider();

            var entryPointInstance = serviceProvider.GetRequiredService(entryPointType);
            var configureServicesArguments = serviceProvider
                .ResolveParameters(configureServicesMethod);

            _ = configureServicesMethod.Invoke(entryPointInstance, configureServicesArguments);

            return servicesRegisteredByPlugin;
        }
    }
}