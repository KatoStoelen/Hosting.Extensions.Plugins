using System;
using Hosting.Extensions.Plugins.NuGetRegistry.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Hosting.Extensions.Plugins.NuGetRegistry
{
    /// <summary>
    /// <see cref="IPluginsBuilder"/> extensions for configuring a NuGet
    /// plugin registry.
    /// </summary>
    public static class IPluginsBuilderExtensions
    {
        /// <summary>
        /// Use the specified NuGet feed as plugin registry.
        /// </summary>
        /// <param name="builder">The <see cref="IPluginsBuilder"/> instance.</param>
        /// <param name="feedConfiguration">The configuration of the NuGet feed to use.</param>
        /// <param name="monitorForChanges">
        /// Whether or not to monitor the NuGet feed for changes to plugin packages.
        /// 
        /// <para>
        /// Defaults to <see langword="false"/>.
        /// </para>
        /// </param>
        /// <param name="monitoringInterval">
        /// When monitoring is enabled, defines the interval between each check for updates.
        /// 
        /// <para>
        /// Defaults to 20 seconds.
        /// </para>
        /// </param>
        /// <returns>The current <see cref="IPluginsBuilder"/> instance for chaining purposes.</returns>
        public static IPluginsBuilder WithNuGetRegistry(
            this IPluginsBuilder builder,
            NuGetFeedConfiguration feedConfiguration,
            bool monitorForChanges = false,
            TimeSpan? monitoringInterval = null)
        {
            feedConfiguration.MakeSourceAbsoluteIfRelativeLocalPath(
                builder.HostContext.HostingEnvironment.ContentRootPath);

            _ = builder
                .WithRegistry<NuGetPluginRegistry>(monitorForChanges)
                .Services
                .Configure<RegistryConfiguration>(
                    config => config.MonitoringInterval = monitoringInterval ?? TimeSpan.FromSeconds(20))
                .AddSingleton<NuGetFeedMonitor>()
                .AddSingleton<NuGetPackageDownloader>()
                .AddSingleton<NuGetPackageLister>()
                .AddSingleton(_ => PackageLockFile.Load())
                .AddSingleton(feedConfiguration)
                .AddSingleton<NuGetLogger>();

            return builder;
        }
    }
}