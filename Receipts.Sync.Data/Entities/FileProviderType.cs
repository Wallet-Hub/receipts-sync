namespace Receipts.Sync.Data.Entities
{
    /// <summary>
    /// Kind of storage a <see cref="FileProvider"/> reads receipt files from.
    /// </summary>
    public enum FileProviderType
    {
        PhysicalFolder,
        GoogleDrive,
        AzureBlobStorage
    }
}
