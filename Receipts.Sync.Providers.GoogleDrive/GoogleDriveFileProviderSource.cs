using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;
using Receipts.Sync.Providers.Configuration;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using DriveFileList = Google.Apis.Drive.v3.Data.FileList;

namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// <see cref="IFileProviderSource"/> for <see cref="FileProviderType.GoogleDrive"/>.
    /// The file id is used as <see cref="ProviderFileInfo.ExternalId"/>.
    /// </summary>
    public sealed class GoogleDriveFileProviderSource : IFileProviderSource, IDisposable
    {
        private const string FileFields = "nextPageToken, files(id, name, mimeType, size, createdTime, modifiedTime, webViewLink)";

        private const string FolderMimeType = "application/vnd.google-apps.folder";
        private const string GoogleWorkspaceMimeTypePrefix = "application/vnd.google-apps.";

        private readonly ConcurrentDictionary<(int ProviderId, string Configuration), DriveService> _services = new();
        private readonly GoogleDriveCredentialFactory _credentialFactory;

        public GoogleDriveFileProviderSource(GoogleDriveCredentialFactory credentialFactory)
        {
            ArgumentNullException.ThrowIfNull(credentialFactory);
            _credentialFactory = credentialFactory;
        }

        public FileProviderType ProviderType => FileProviderType.GoogleDrive;

        public async IAsyncEnumerable<ProviderFileInfo> EnumerateFilesAsync(ProviderContext provider, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            GoogleDriveOptions options = ProviderConfiguration.Read<GoogleDriveOptions>(provider);
            DriveService service = await GetServiceAsync(provider, options, cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.Query))
            {
                await foreach (DriveFile? file in ListFilesAsync(service, options.Query, options, cancellationToken))
                {
                    if (TryMapFile(file, options, out ProviderFileInfo info))
                    {
                        yield return info;
                    }
                }

                yield break;
            }

            if (string.IsNullOrWhiteSpace(options.FolderId))
            {
                await foreach (DriveFile? file in ListFilesAsync(service, "trashed = false", options, cancellationToken))
                {
                    if (TryMapFile(file, options, out ProviderFileInfo info))
                    {
                        yield return info;
                    }
                }

                yield break;
            }

            string rootFolderId = ValidateFolderId(provider, options.FolderId);
            var pendingFolders = new Queue<string>();
            var visitedFolders = new HashSet<string>(StringComparer.Ordinal);
            pendingFolders.Enqueue(rootFolderId);

            while (pendingFolders.Count > 0)
            {
                string folderId = pendingFolders.Dequeue();
                if (!visitedFolders.Add(folderId))
                {
                    continue;
                }

                string folderQuery = $"'{folderId}' in parents and trashed = false";
                await foreach (DriveFile file in ListFilesAsync(service, folderQuery, options, cancellationToken))
                {
                    if (string.Equals(file.MimeType, FolderMimeType, StringComparison.Ordinal))
                    {
                        if (options.Recursive && !string.IsNullOrEmpty(file.Id))
                        {
                            pendingFolders.Enqueue(file.Id);
                        }

                        continue;
                    }

                    if (TryMapFile(file, options, out ProviderFileInfo info))
                    {
                        yield return info;
                    }
                }
            }
        }

        public async Task<Stream> OpenReadAsync(ProviderContext provider, ProviderFileInfo file, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);

            GoogleDriveOptions options = ProviderConfiguration.Read<GoogleDriveOptions>(provider);
            DriveService service = await GetServiceAsync(provider, options, cancellationToken);

            FilesResource.GetRequest request = service.Files.Get(file.ExternalId);
            request.SupportsAllDrives = options.IncludeSharedDrives;

            // The Drive API can only download into a writable stream, so the content is spooled to a
            // temporary file that is removed as soon as the caller disposes the returned stream.
            string tempPath = Path.Combine(Path.GetTempPath(), $"receipts-sync-{Guid.NewGuid():N}.tmp");
            var stream = new FileStream(tempPath, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.ReadWrite,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose,
                BufferSize = 81920,
            });

            try
            {
                await request.DownloadAsync(stream, cancellationToken);
                stream.Position = 0;
                return stream;
            }
            catch
            {
                await stream.DisposeAsync();
                throw;
            }
        }

        public void Dispose()
        {
            foreach (DriveService service in _services.Values)
            {
                service.Dispose();
            }

            _services.Clear();
        }

        private static async IAsyncEnumerable<DriveFile> ListFilesAsync(DriveService service, string query, GoogleDriveOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string? pageToken = null;

            do
            {
                FilesResource.ListRequest request = service.Files.List();
                request.Q = query;
                request.Fields = FileFields;
                request.PageSize = 1000;
                request.PageToken = pageToken;
                request.IncludeItemsFromAllDrives = options.IncludeSharedDrives;
                request.SupportsAllDrives = options.IncludeSharedDrives;

                DriveFileList page = await request.ExecuteAsync(cancellationToken);
                if (page.Files is not null)
                {         
                    foreach (DriveFile file in page.Files)
                    {
                        yield return file;
                    }
                }

                pageToken = page.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));
        }

        private static bool TryMapFile(DriveFile file, GoogleDriveOptions options, out ProviderFileInfo info)
        {
            info = null!;

            if (string.IsNullOrEmpty(file.Id))
            {
                return false;
            }

            // Google Workspace documents (Docs, Sheets, ...) have no binary content to hash.
            if (file.MimeType is not null && file.MimeType.StartsWith(GoogleWorkspaceMimeTypePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!FileExtensionFilter.Matches(file.Name, options.Extensions))
            {
                return false;
            }

            info = new ProviderFileInfo
            {
                ExternalId = file.Id,
                FileName = file.Name,
                ContentType = file.MimeType,
                SizeInBytes = file.Size,
                Url = Uri.TryCreate(file.WebViewLink, UriKind.Absolute, out Uri? url) ? url : null,
                CreatedAt = file.CreatedTimeDateTimeOffset?.UtcDateTime,
                UpdatedAt = file.ModifiedTimeDateTimeOffset?.UtcDateTime,
            };

            return true;
        }

        private static string ValidateFolderId(ProviderContext provider, string folderId)
        {
            foreach (char character in folderId)
            {
                if (!char.IsLetterOrDigit(character) && character is not ('-' or '_'))
                {
                    throw new FileProviderConfigurationException($"The 'folderId' of provider '{provider.Name}' contains invalid characters.");
                }
            }

            return folderId;
        }

        private async Task<DriveService> GetServiceAsync(ProviderContext provider, GoogleDriveOptions options, CancellationToken cancellationToken)
        {
            (int ProviderId, string Configuration) key = (provider.Id, provider.Configuration ?? string.Empty);
            if (_services.TryGetValue(key, out DriveService? existingService))
            {
                return existingService;
            }

            DriveService createdService = await CreateServiceAsync(provider, options, cancellationToken);
            if (_services.TryAdd(key, createdService))
            {
                return createdService;
            }

            createdService.Dispose();
            return _services[key];
        }

        private async Task<DriveService> CreateServiceAsync(ProviderContext provider, GoogleDriveOptions options, CancellationToken cancellationToken)
        {
            GoogleCredential credential = await _credentialFactory.CreateAsync(provider, options, cancellationToken);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = options.ApplicationName,
            });
        }
    }
}
