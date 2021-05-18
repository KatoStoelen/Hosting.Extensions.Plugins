using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class PluginsSettings
    {
        public string RootDirectory { get; set; } = string.Empty;
        public bool MonitorRootDirectory { get; set; }
        public bool FailOnMissingPlugins { get; set; }
        public bool MonitorRegistry { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public HashSet<Assembly> SharedAssemblies { get; } = new HashSet<Assembly>
        {
            typeof(IHostEnvironment).Assembly,
            typeof(IFileProvider).Assembly,
            typeof(IServiceCollection).Assembly
        };
        public HashSet<Type> ContractTypes { get; } = new HashSet<Type>();
        public Func<DirectoryInfo, FileInfo>? CustomMainAssemblySelector { get; set; }
        public Action<IHostEnvironment>? CustomHostEnvironmentConfigurer { get; set; }
        public Action<IServiceCollection>? CustomPluginServicesConfigurer { get; set; }

        internal FileInfo GetMainPluginAssembly(DirectoryInfo pluginDirectory)
        {
            return CustomMainAssemblySelector != null
                ? CustomMainAssemblySelector.Invoke(pluginDirectory)
                : new FileInfo(
                    Path.Combine(
                        pluginDirectory.FullName,
                        pluginDirectory.Name + ".dll"));
        }

        internal PluginLoader GetPluginLoader(FileInfo mainPluginAssembly)
        {
            var config = new PluginConfig(mainPluginAssembly.FullName)
            {
                EnableHotReload = true
            };

            foreach (var assembly in SharedAssemblies)
            {
                config.SharedAssemblies.Add(assembly.GetName());
            }

            return new PluginLoader(config);
        }

        internal IHostEnvironment GetPluginHostEnvironment(DirectoryInfo pluginDirectory)
        {
            var hostEnvironment = new PluginHostEnvironment(
                EnvironmentName,
                ApplicationName,
                pluginDirectory.FullName,
                new PhysicalFileProvider(pluginDirectory.FullName));

            CustomHostEnvironmentConfigurer?.Invoke(hostEnvironment);

            return hostEnvironment;
        }
    }
}