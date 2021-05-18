using System;
using Hosting.Extensions.Plugins.Internal;
using Microsoft.Extensions.Hosting;

namespace Hosting.Extensions.Plugins
{
    /// <summary>
    /// Plugin extensions of <see cref="IHostBuilder"/>.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures plugins to be loaded into the current host.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="configure">A delegate configuring plugin settings.</param>
        /// <returns>The current <see cref="IHostBuilder"/> instance for chaining purposes.</returns>
        public static IHostBuilder ConfigurePlugins(
            this IHostBuilder hostBuilder, Action<IPluginsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return hostBuilder.ConfigurePlugins((_, pluginsBuilder) => configure(pluginsBuilder));
        }

        /// <summary>
        /// Configures plugins to be loaded into the current host.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="configure">A delegate configuring plugin settings.</param>
        /// <returns>The current <see cref="IHostBuilder"/> instance for chaining purposes.</returns>
        public static IHostBuilder ConfigurePlugins(
            this IHostBuilder hostBuilder, Action<HostBuilderContext, IPluginsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return hostBuilder.ConfigureServices((context, services) =>
            {
                var pluginsBuilder = PluginsBuilder
                    .CreateDefaultBuilder(services, context);

                configure.Invoke(context, pluginsBuilder);
            });
        }
    }
}
