using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Hosting.Extensions.Plugins.NuGetRegistry.Internal
{
    internal class PackageLockFile
    {
        private static readonly JsonSerializerOptions s_serializerOptions =
            new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        private static readonly string s_filePath = Path.Combine(
            Path.GetTempPath(), "plugin.nugetregistry.lock");

        private readonly List<PackageEntry> _packages;
        private readonly IVersionComparer _versionComparer;
        private bool _isDirty;

        private PackageLockFile(List<PackageEntry> packages)
        {
            _packages = packages;
            _versionComparer = VersionComparer.VersionReleaseMetadata;
        }

        public IEnumerable<PackageIdentity> Update(IEnumerable<PackageIdentity> newPackages)
        {
            foreach (var package in newPackages)
            {
                if (Update(package))
                {
                    yield return package;
                }
            }
        }

        public bool Update(PackageIdentity package)
        {
            var updateAction = GetUpdateAction(package);

            if (updateAction == UpdateAction.None)
            {
                return false;
            }

            Update(package, updateAction);

            return true;
        }

        private UpdateAction GetUpdateAction(PackageIdentity package)
        {
            var currentPackage = _packages.SingleOrDefault(
                pkg => pkg.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase));

            if (currentPackage == null)
            {
                return UpdateAction.Add;
            }

            if (_versionComparer.Compare(package.Version, currentPackage.SemVer) > 0)
            {
                return UpdateAction.Update;
            }

            return UpdateAction.None;
        }

        private void Update(PackageIdentity package, UpdateAction action)
        {
            if (action == UpdateAction.Update)
            {
                var currentPackage = _packages.Single(
                    pkg => pkg.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase));

                _ = _packages.Remove(currentPackage);
            }

            _packages.Add(new PackageEntry(package));

            _isDirty = true;
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (!_isDirty)
            {
                return;
            }

            using var fileStream = new FileStream(
                s_filePath, FileMode.Create, FileAccess.Write);

            await JsonSerializer
                .SerializeAsync(fileStream, _packages, s_serializerOptions, cancellationToken)
                .ConfigureAwait(false);

            _isDirty = false;
        }

        public static PackageLockFile Load()
        {
            if (!File.Exists(s_filePath))
            {
                return new PackageLockFile(new List<PackageEntry>());
            }

            try
            {
                var jsonBytes = File.ReadAllBytes(s_filePath);

                var packages = JsonSerializer.Deserialize<List<PackageEntry>>(
                    jsonBytes, s_serializerOptions);

                return new PackageLockFile(packages ?? new List<PackageEntry>());
            }
            catch (Exception)
            {
                return new PackageLockFile(new List<PackageEntry>());
            }
        }

        private class PackageEntry
        {
            [JsonConstructor]
            public PackageEntry(string id, string version)
            {
                Id = id;
                Version = version;
                SemVer = SemanticVersion.Parse(version);
            }

            public PackageEntry(PackageIdentity package)
            {
                Id = package.Id;
                Version = package.Version.ToNormalizedString();
                SemVer = package.Version;
            }

            public string Id { get; }
            public string Version { get; }

            [JsonIgnore]
            public SemanticVersion SemVer { get; }
        }

        private enum UpdateAction
        {
            None = 0,
            Add = 1,
            Update = 2
        }
    }
}