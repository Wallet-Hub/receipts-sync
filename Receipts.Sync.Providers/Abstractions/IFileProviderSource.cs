using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Providers.Abstractions
{
    /// <summary>
    /// Reads the files exposed by a concrete storage backend (local folder, Google Drive, ...).
    /// Implementations are resolved by <see cref="FileProviderType"/>, so a new backend only needs
    /// a new implementation of this interface and a DI registration.
    /// </summary>
    public interface IFileProviderSource
    {
        /// <summary>
        /// Storage backend handled by this implementation.
        /// </summary>
        FileProviderType ProviderType { get; }

        /// <summary>
        /// Enumerates the files currently available on the provider described by <paramref name="provider"/>.
        /// The provider-specific settings are read from <see cref="ProviderContext.Configuration"/>.
        /// </summary>
        IAsyncEnumerable<ProviderFileInfo> EnumerateFilesAsync(ProviderContext provider, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens the content of a file previously returned by <see cref="EnumerateFilesAsync"/>, so the
        /// caller can compute its hash. The caller owns the returned stream.
        /// </summary>
        Task<Stream> OpenReadAsync(ProviderContext provider, ProviderFileInfo file, CancellationToken cancellationToken = default);
    }
}
