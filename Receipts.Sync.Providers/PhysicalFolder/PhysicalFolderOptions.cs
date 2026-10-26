namespace Receipts.Sync.Providers.PhysicalFolder
{
    /// <summary>
    /// Settings stored in <see cref="Receipts.Sync.Providers.Abstractions.ProviderContext.Configuration"/> for
    /// <see cref="Data.Entities.FileProviderType.PhysicalFolder"/> providers.
    /// </summary>
    public sealed class PhysicalFolderOptions
    {
        /// <summary>
        /// Root folder to scan. Environment variables (e.g. <c>%USERPROFILE%</c>) are expanded.
        /// </summary>
        public string? RootPath { get; set; }

        /// <summary>
        /// Whether sub folders are scanned too. Defaults to <c>true</c>.
        /// </summary>
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// Optional list of accepted extensions (e.g. <c>[".pdf", "jpg"]</c>). Empty means every file.
        /// </summary>
        public IReadOnlyList<string>? Extensions { get; set; }
    }
}
