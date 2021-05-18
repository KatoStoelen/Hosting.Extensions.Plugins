using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Hosting.Extensions.Plugins.NuGetRegistry.Internal
{
    internal class NuGetRegistryEntry : IPluginRegistryEntry
    {
        private readonly PackageIdentity _package;
        private readonly NuGetPackageDownloader _packageDownloader;

        public NuGetRegistryEntry(
            PackageIdentity package, NuGetPackageDownloader packageDownloader)
        {
            _package = package;
            _packageDownloader = packageDownloader;
        }

        public string PluginName => _package.Id;

        public SemanticVersion Version => _package.Version;

        public Task CopyToAsync(DirectoryInfo directory, CancellationToken cancellationToken)
        {
            return _packageDownloader.DownloadAsync(_package, directory, cancellationToken);
        }
    }
}