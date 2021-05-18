using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hosting.Extensions.Plugins.NuGetRegistry.Internal
{
    internal class NuGetPluginRegistry : IPluginRegistry
    {
        private readonly NuGetPackageLister _packageLister;
        private readonly NuGetPackageDownloader _packageDownloader;
        private readonly NuGetFeedMonitor _feedMonitor;
        private readonly PackageLockFile _lockFile;

        public NuGetPluginRegistry(
            NuGetPackageLister packageLister,
            NuGetPackageDownloader packageDownloader,
            NuGetFeedMonitor feedMonitor,
            PackageLockFile lockFile)
        {
            _packageLister = packageLister;
            _packageDownloader = packageDownloader;
            _feedMonitor = feedMonitor;
            _lockFile = lockFile;
        }

        public bool CanMonitor => true;

        public async IAsyncEnumerable<IPluginRegistryEntry> GetLatestVersionOfEntries(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var packages = _packageLister
                .ListLatestAsync(cancellationToken);

            await foreach (var package in packages.ConfigureAwait(false))
            {
                _ = _lockFile.Update(package.Identity);

                yield return new NuGetRegistryEntry(package.Identity, _packageDownloader);
            }

            await _lockFile.SaveAsync(cancellationToken).ConfigureAwait(false);
        }

        public IAsyncDisposable StartMonitoring(Action<IPluginRegistryEntry> onEntryChanged)
        {
            _feedMonitor.Start(onEntryChanged);

            return new StartedMonitor(_feedMonitor);
        }

        private class StartedMonitor : IAsyncDisposable
        {
            private readonly NuGetFeedMonitor _feedMonitor;

            public StartedMonitor(NuGetFeedMonitor feedMonitor)
            {
                _feedMonitor = feedMonitor;
            }

            public async ValueTask DisposeAsync()
            {
                await _feedMonitor.StopAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}