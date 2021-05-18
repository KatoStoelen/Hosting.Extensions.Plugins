using System;
using System.IO;
using System.Reflection;
using Hosting.Extensions.Plugins.Internal.Extensions;
using Microsoft.Extensions.DependencyInjection;
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

            foreach (var customPluginServiceDescriptor in _settings.CustomPluginServices)
            {
                services.Add(customPluginServiceDescriptor);
            }

            var pluginAssemblyEntryPoint = new PluginAssemblyEntryPoint(mainPluginAssembly);

            if (pluginAssemblyEntryPoint.Exists)
            {
                pluginAssemblyEntryPoint.ConfigureServices(services);
            }
            else
            {
                _ = services.AddTypesFromAssembly(mainPluginAssembly);
            }

            return services.BuildServiceProvider();
        }
    }
}