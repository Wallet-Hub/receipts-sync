namespace Receipts.Sync.Providers.Abstractions
{
    /// <summary>
    /// Raised when the <see cref="Receipts.Sync.Providers.Abstractions.ProviderContext.Configuration"/> of a provider
    /// is missing or cannot be used by its <see cref="IFileProviderSource"/>.
    /// </summary>
    public sealed class FileProviderConfigurationException : Exception
    {
        public FileProviderConfigurationException(string message)
            : base(message)
        {
        }

        public FileProviderConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
