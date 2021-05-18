using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hosting.Extensions.Plugins.NuGetRegistry.Internal
{
    internal class NuGetFeedMonitor : IDisposable
    {
        private readonly SemaphoreSlim _startSemaphore = new(initialCount: 1, maxCount: 1);
        private readonly TimeSpan _interval;
        private readonly NuGetPackageLister _packageLister;
        private readonly PackageLockFile _lockFile;
        private readonly NuGetPackageDownloader _packageDownloader;
        private readonly ILogger<NuGetFeedMonitor> _logger;

        private Task? _monitorTask;
        private CancellationTokenSource? _cts;

        public NuGetFeedMonitor(
            IOptions<RegistryConfiguration> configuration,
            NuGetPackageLister packageLister,
            PackageLockFile lockFile,
            NuGetPackageDownloader packageDownloader,
            ILogger<NuGetFeedMonitor> logger)
        {
            _interval = configuration.Value.MonitoringInterval;
            _packageLister = packageLister;
            _lockFile = lockFile;
            _packageDownloader = packageDownloader;
            _logger = logger;
        }

        public void Start(Action<IPluginRegistryEntry> callback)
        {
            _startSemaphore.Wait();

            try
            {
                if (_monitorTask != null)
                {
                    throw new InvalidOperationException($"Monitor already started");
                }

                _logger.LogDebug(
                    "Starting monitoring of NuGet feed (interval: {Interval}): {Source}",
                    _interval,
                    _packageLister.Source);

                _cts = new CancellationTokenSource();
                _monitorTask = MonitorAsync(callback, _cts.Token);
            }
            finally
            {
                _ = _startSemaphore.Release();
            }
        }

        public async Task StopAsync()
        {
            if (_monitorTask == null)
            {
                return;
            }

            _logger.LogDebug("Stopping monitoring of NuGet feed");

            _cts!.Cancel();

            try
            {
                await _monitorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            _monitorTask = null;

            _cts.Dispose();
            _cts = null;
        }

        private async Task MonitorAsync(
            Action<IPluginRegistryEntry> callback, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Looking for updated plugins");

                var latestPackages = (await _packageLister
                        .ListLatestAsync(cancellationToken)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false))
                    .Select(package => package.Identity)
                    .ToList();

                _logger.LogTrace(
                    "Latest packages:{NewLine}{Packages}",
                    Environment.NewLine,
                    string.Join(Environment.NewLine, latestPackages));

                var updatedPackages = _lockFile
                    .Update(latestPackages)
                    .ToList();

                _logger.LogTrace(
                    "Updated packages:{NewLine}{Packages}",
                    Environment.NewLine,
                    string.Join(Environment.NewLine, updatedPackages));

                foreach (var package in updatedPackages)
                {
                    callback(new NuGetRegistryEntry(package, _packageDownloader));
                }

                await _lockFile.SaveAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}