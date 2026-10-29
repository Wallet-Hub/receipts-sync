using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// Reads Google Drive credentials from a local JSON file.
    /// </summary>
    public sealed class FileGoogleDriveCredentialsProvider : IGoogleDriveCredentialsProvider
    {
        /// <inheritdoc />
        public Task<Stream> OpenReadAsync(ProviderContext provider, GoogleDriveOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(options);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(options.CredentialsPath))
            {
                throw new FileProviderConfigurationException(
                    $"Provider '{provider.Name}' requires 'credentialsPath'. Credentials must not be stored in the provider configuration.");
            }

            string path = Path.GetFullPath(options.CredentialsPath);
            if (!File.Exists(path))
            {
                throw new FileProviderConfigurationException($"The credentials file '{path}' of provider '{provider.Name}' does not exist.");
            }

            Stream stream = new FileStream(path, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });

            return Task.FromResult(stream);
        }
    }
}
