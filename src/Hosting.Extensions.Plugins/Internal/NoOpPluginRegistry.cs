using System;
using System.Collections.Generic;
using System.Linq;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class NoOpPluginRegistry : IPluginRegistry
    {
        public bool ShouldMonitorForChanges => false;

        public IAsyncEnumerable<IPluginRegistryEntry> GetEntries()
        {
            return AsyncEnumerable.Empty<IPluginRegistryEntry>();
        }

        public IDisposable StartMonitoring(Action<IPluginRegistryEntry> onEntryChanged)
        {
            throw new InvalidOperationException("Not supported");
        }
    }
}