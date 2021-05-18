using System;
using System.IO;
using System.Text.RegularExpressions;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Hosting.Extensions.Plugins.NuGetRegistry
{
    /// <summary>
    /// Defines the NuGet feed for this plugin registry.
    /// </summary>
    public class NuGetFeedConfiguration
    {
        /// <summary>
        /// Creates a new NuGet feed configuration with the specified URL.
        /// </summary>
        /// <param name="source">The NuGet feed source.</param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="source"/> is null, empty or whitespace.
        /// </exception>
        public NuGetFeedConfiguration(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException($"Source must be set", nameof(source));
            }

            Source = source;
        }

        /// <summary>
        /// The NuGet feed source.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// The username to use when connecting to the feed.
        /// </summary>
        public string? Username { get; init; }

        /// <summary>
        /// The password to use when connecting to the feed.
        /// </summary>
        public string? Password { get; init; }

        /// <summary>
        /// Whether or not to include pre-releases of plugins.
        /// 
        /// <para>
        /// Defaults to <see langword="false"/>.
        /// </para>
        /// </summary>
        public bool IncludePrereleases { get; init; }

        internal SourceRepository GetSourceRepository()
        {
            var packageSource = new PackageSource(Source)
            {
                Credentials = !string.IsNullOrWhiteSpace(Username)
                    ? PackageSourceCredential.FromUserInput(
                        Source,
                        Username,
                        Password,
                        storePasswordInClearText: true,
                        validAuthenticationTypesText: null)
                    : null
            };

            return Repository.Factory.GetCoreV3(packageSource);
        }

        internal void MakeSourceAbsoluteIfRelativeLocalPath(string contentRootPath)
        {
            var isHttp = Regex.IsMatch(Source, @"^https?://");

            if (isHttp)
            {
                return;
            }

            Source = Environment.ExpandEnvironmentVariables(Source);

            if (Path.IsPathRooted(Source))
            {
                return;
            }

            Source = Path.GetFullPath(Path.Combine(contentRootPath, Source));
        }
    }
}