using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace Hosting.Extensions.Plugins.NuGetRegistry.Internal
{
    internal class NuGetPackageDownloader
    {
        private readonly NuGetFeedConfiguration _feedConfiguration;
        private readonly NuGetLogger _nuGetLogger;
        private readonly ILogger<NuGetPackageDownloader> _logger;

        public NuGetPackageDownloader(
            NuGetFeedConfiguration feedConfiguration,
            NuGetLogger nuGetLogger,
            ILogger<NuGetPackageDownloader> logger)
        {
            _feedConfiguration = feedConfiguration;
            _nuGetLogger = nuGetLogger;
            _logger = logger;
        }

        public async Task DownloadAsync(
            PackageIdentity package,
            DirectoryInfo downloadDirectory,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                "Downloading package {Package} from {Feed}",
                package,
                _feedConfiguration.Source);

            var sourceRepository = _feedConfiguration.GetSourceRepository();

            var downloadResource = await sourceRepository
                .GetResourceAsync<DownloadResource>(cancellationToken)
                .ConfigureAwait(false);

            using var downloadResult = await downloadResource
                .GetDownloadResourceResultAsync(
                    package,
                    new PackageDownloadContext(new SourceCacheContext()),
                    SettingsUtility.GetGlobalPackagesFolder(
                        Settings.LoadDefaultSettings(root: null)),
                    _nuGetLogger,
                    cancellationToken)
                .ConfigureAwait(false);

            if (downloadResult.Status == DownloadResourceResultStatus.Cancelled)
            {
                throw new OperationCanceledException();
            }

            if (downloadResult.Status != DownloadResourceResultStatus.Available)
            {
                throw new PackageDownloadException(
                    $"Failed to download package {package} ({downloadResult.Status})");
            }

            using var memoryStream = new MemoryStream((int)downloadResult.PackageStream.Length);

            await downloadResult.PackageStream
                .CopyToAsync(memoryStream, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "Extracting package {Package} to {Directory}",
                package,
                downloadDirectory.FullName);

            using var zipArchive = new ZipArchive(memoryStream);

            await Task
                .Run(
                    () => zipArchive.ExtractToDirectory(
                        downloadDirectory.FullName, overwriteFiles: true),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private class PackageDownloadException : Exception
        {
            public PackageDownloadException(string message)
                : base(message)
            {
            }
        }
    }
}