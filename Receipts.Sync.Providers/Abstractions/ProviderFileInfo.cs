namespace Receipts.Sync.Providers.Abstractions
{
    /// <summary>
    /// Metadata of a file discovered on a <see cref="ProviderContext"/>.
    /// Provider specific details (paths, drive ids, ...) must be mapped into <see cref="ExternalId"/>
    /// only, so the synchronization pipeline stays backend agnostic.
    /// </summary>
    public sealed class ProviderFileInfo
    {
        /// <summary>
        /// Identifier of the file within the provider (relative path, drive file id, blob name).
        /// </summary>
        public required string ExternalId { get; init; }

        public string? FileName { get; init; }

        /// <summary>
        /// MIME type reported by the provider (or guessed from the file name).
        /// </summary>
        public string? ContentType { get; init; }

        public long? SizeInBytes { get; init; }

        /// <summary>
        /// Location of the file, when the provider exposes one. Local folders return <c>null</c>.
        /// </summary>
        public Uri? Url { get; init; }

        /// <summary>
        /// Creation timestamp reported by the provider. <c>null</c> when unavailable.
        /// </summary>
        public DateTime? CreatedAt { get; init; }

        /// <summary>
        /// Last modification timestamp reported by the provider. Used to skip re-hashing unchanged files.
        /// </summary>
        public DateTime? UpdatedAt { get; init; }
    }
}
