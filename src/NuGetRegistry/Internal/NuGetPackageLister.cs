using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NuGet.Protocol.Core.Types;

namespace Hosting.Extensions.Plugins.NuGetRegistry.Internal
{
    internal class NuGetPackageLister
    {
        private readonly NuGetFeedConfiguration _feedConfiguration;
        private readonly NuGetLogger _nuGetLogger;

        public NuGetPackageLister(
            NuGetFeedConfiguration feedConfiguration, NuGetLogger nuGetLogger)
        {
            _feedConfiguration = feedConfiguration;
            _nuGetLogger = nuGetLogger;
        }

        public string Source => _feedConfiguration.Source;

        public async IAsyncEnumerable<IPackageSearchMetadata> ListLatestAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sourceRepository = _feedConfiguration.GetSourceRepository();

            var feed = await sourceRepository
                .GetResourceAsync<ListResource>(cancellationToken)
                .ConfigureAwait(false);

            var packages = await feed
                .ListAsync(
                    searchTerm: null,
                    _feedConfiguration.IncludePrereleases,
                    allVersions: false,
                    includeDelisted: false,
                    _nuGetLogger,
                    cancellationToken)
                .ConfigureAwait(false);

            var packageEnumerator = packages.GetEnumeratorAsync();

            while (await packageEnumerator.MoveNextAsync().ConfigureAwait(false))
            {
                yield return packageEnumerator.Current;

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}