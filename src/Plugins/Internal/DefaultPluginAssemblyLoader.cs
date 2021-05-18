using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Hosting.Extensions.Plugins.Internal.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class DefaultPluginAssemblyLoader : IPluginAssemblyLoader
    {
        private readonly PluginsSettings _settings;

        public DefaultPluginAssemblyLoader(IOptions<PluginsSettings> settings)
        {
            _settings = settings.Value;
        }

        public IServiceProvider Load(Assembly mainPluginAssembly, DirectoryInfo pluginDirectory)
        {
            var pluginHostEnvironment = _settings.GetPluginHostEnvironment(pluginDirectory);

            var services = new ServiceCollection()
                .AddSingleton(pluginHostEnvironment)
                .AddSingleton(pluginHostEnvironment.ContentRootFileProvider);

            _settings.CustomPluginServicesConfigurer?.Invoke(services);

            var pluginAssemblyEntryPoint = new PluginAssemblyEntryPoint(mainPluginAssembly);

            if (pluginAssemblyEntryPoint.Exists)
            {
                var servicesAddedByPlugin = pluginAssemblyEntryPoint
                    .ConfigureServices(services);

                EnsureRegisteredAsContractTypes(servicesAddedByPlugin);

                services.Add(servicesAddedByPlugin);
            }
            else
            {
                _ = services.AddTypesFromAssembly(mainPluginAssembly);
            }

            return services.BuildServiceProvider();
        }

        private void EnsureRegisteredAsContractTypes(IServiceCollection servicesAddedByPlugin)
        {
            var contractTypes = _settings.ContractTypes;
            var servicesSnapshot = servicesAddedByPlugin.ToList();

            foreach (var descriptor in servicesSnapshot)
            {
                var assignableToContracts = contractTypes
                    .Where(contractType =>
                        contractType != descriptor.ServiceType &&
                        descriptor.ServiceType.IsAssignableTo(contractType));

                foreach (var contractType in assignableToContracts)
                {
                    var alreadyRegistered = servicesAddedByPlugin
                        .Any(service =>
                            service.ServiceType == contractType &&
                            service.GetImplementationType() == descriptor.GetImplementationType());

                    if (alreadyRegistered)
                    {
                        continue;
                    }

                    servicesAddedByPlugin.Add(
                        ServiceDescriptor.Describe(
                            contractType,
                            provider => provider.GetRequiredService(descriptor.ServiceType),
                            descriptor.Lifetime));
                }
            }
        }
    }
}