using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// Opens the credential document used to authenticate a Google Drive provider.
    /// </summary>
    public interface IGoogleDriveCredentialsProvider
    {
        /// <summary>
        /// Opens a readable stream containing the credentials for <paramref name="provider"/>.
        /// The caller owns and disposes the returned stream.
        /// </summary>
        Task<Stream> OpenReadAsync(ProviderContext provider, GoogleDriveOptions options, CancellationToken cancellationToken = default);
    }
}
