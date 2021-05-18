using System;
using System.Collections.Generic;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class ImplementationResolver<TPluginContract> : IPlugins<TPluginContract>
    {
        private readonly PluginCollection _plugins;

        public ImplementationResolver(PluginCollection plugins)
        {
            _plugins = plugins;
        }

        public IReadOnlyCollection<TPluginContract> Plugins =>
            _plugins.GetImplementationsOf<TPluginContract>();

        public IReadOnlyCollection<Type> Types =>
            _plugins.GetTypesImplementing<TPluginContract>();
    }
}