using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;
using Receipts.Sync.Providers.Configuration;

namespace Receipts.Sync.Providers.PhysicalFolder
{
    /// <summary>
    /// <see cref="IFileProviderSource"/> for <see cref="FileProviderType.PhysicalFolder"/>:
    /// scans a directory tree on the machine running the synchronization.
    /// </summary>
    public sealed class PhysicalFolderFileProviderSource : IFileProviderSource
    {
        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        private readonly ILogger<PhysicalFolderFileProviderSource> _logger;

        public PhysicalFolderFileProviderSource(ILogger<PhysicalFolderFileProviderSource> logger)
        {
            _logger = logger;
        }

        public FileProviderType ProviderType => FileProviderType.PhysicalFolder;

        public async IAsyncEnumerable<ProviderFileInfo> EnumerateFilesAsync(
            ProviderContext provider,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            PhysicalFolderOptions options = ProviderConfiguration.Read<PhysicalFolderOptions>(provider);
            string rootPath = ResolveRootPath(provider, options);

            if (!Directory.Exists(rootPath))
            {
                throw new FileProviderConfigurationException(
                    $"The root folder '{rootPath}' of provider '{provider.Name}' does not exist.");
            }

            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = options.Recursive,
                IgnoreInaccessible = true,
                MatchType = MatchType.Simple,
            };

            foreach (var path in Directory.EnumerateFiles(rootPath, "*", enumerationOptions))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? fileName = Path.GetFileName(path);
                if (!FileExtensionFilter.Matches(fileName, options.Extensions))
                {
                    continue;
                }

                long sizeInBytes;
                DateTime createdAt;
                DateTime updatedAt;

                try
                {
                    var file = new FileInfo(path);
                    if (!file.Exists)
                        continue;

                    sizeInBytes = file.Length;
                    createdAt = file.CreationTimeUtc;
                    updatedAt = file.LastWriteTimeUtc;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(
                        ex,
                        "Skipping '{Path}' of provider '{ProviderName}': the file cannot be read.",
                        path,
                        provider.Name);
                    continue;
                }

                yield return new ProviderFileInfo
                {
                    ExternalId = ToExternalId(rootPath, path),
                    FileName = fileName,
                    ContentType = ContentTypes.FromFileName(fileName),
                    SizeInBytes = sizeInBytes,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                };

                await Task.Yield();
            }
        }

        public Task<Stream> OpenReadAsync(
            ProviderContext provider,
            ProviderFileInfo file,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);

            PhysicalFolderOptions options = ProviderConfiguration.Read<PhysicalFolderOptions>(provider);
            string rootPath = ResolveRootPath(provider, options);
            string relativePath = file.ExternalId.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));

            if (!fullPath.StartsWith(EnsureTrailingSeparator(rootPath), PathComparison))
            {
                throw new FileProviderConfigurationException(
                    $"'{file.ExternalId}' resolves outside the root folder of provider '{provider.Name}'.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            Stream stream = new FileStream(fullPath, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                BufferSize = 81920,
            });

            return Task.FromResult(stream);
        }

        private static string ResolveRootPath(ProviderContext provider, PhysicalFolderOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.RootPath))
            {
                throw new FileProviderConfigurationException(
                    $"Provider '{provider.Name}' requires the 'rootPath' setting.");
            }

            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.RootPath));
        }

        private static string ToExternalId(string rootPath, string path)
        {
            return Path.GetRelativePath(rootPath, path)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static string EnsureTrailingSeparator(string path)
        {
            return Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
        }
    }
}
