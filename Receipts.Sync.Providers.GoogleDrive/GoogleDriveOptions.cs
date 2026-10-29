namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// Settings stored in <see cref="Receipts.Sync.Providers.Abstractions.ProviderContext.Configuration"/> for
    /// <see cref="Data.Entities.FileProviderType.GoogleDrive"/> providers.
    /// </summary>
    public sealed class GoogleDriveOptions
    {
        /// <summary>
        /// Path to a Google credentials JSON file (service account key or authorized user token).
        /// Relative paths are resolved against the current working directory.
        /// </summary>
        public string? CredentialsPath { get; set; }

        /// <summary>
        /// Optional user to impersonate through domain-wide delegation (service accounts only).
        /// </summary>
        public string? ImpersonateUser { get; set; }

        /// <summary>
        /// Id of the folder to scan. When neither this nor <see cref="Query"/> is set, every file visible
        /// to the credentials is scanned.
        /// </summary>
        public string? FolderId { get; set; }

        /// <summary>
        /// Raw Drive query used instead of <see cref="FolderId"/>, e.g.
        /// <c>name contains 'receipt' and trashed = false</c>. Query results are not traversed recursively.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// Whether sub folders of <see cref="FolderId"/> are scanned too. Defaults to <c>true</c>.
        /// </summary>
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// Whether shared drives are searched too. Defaults to <c>true</c>.
        /// </summary>
        public bool IncludeSharedDrives { get; set; } = true;

        /// <summary>
        /// Optional list of accepted extensions (e.g. <c>[".pdf", "jpg"]</c>). Empty means every file.
        /// </summary>
        public IReadOnlyList<string>? Extensions { get; set; }

        /// <summary>
        /// Application name sent to the Drive API. Defaults to <c>receipts-sync</c>.
        /// </summary>
        public string ApplicationName { get; set; } = "receipts-sync";
    }
}
