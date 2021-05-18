using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Hosting.Extensions.Plugins.Internal
{
    internal class NoOpPluginRegistry : IPluginRegistry
    {
        public bool CanMonitor => false;

        public IAsyncEnumerable<IPluginRegistryEntry> GetLatestVersionOfEntries(
            CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<IPluginRegistryEntry>();
        }

        public IAsyncDisposable StartMonitoring(Action<IPluginRegistryEntry> onEntryChanged)
        {
            throw new NotSupportedException("Not supported");
        }
    }
}