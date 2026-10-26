using System.Diagnostics.CodeAnalysis;
using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Providers.Abstractions
{
    /// <summary>
    /// Resolves the <see cref="IFileProviderSource"/> that knows how to read a given provider type.
    /// </summary>
    public interface IFileProviderSourceRegistry
    {
        /// <summary>
        /// Gets the source registered for <paramref name="providerType"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// No source is registered for <paramref name="providerType"/>.
        /// </exception>
        IFileProviderSource GetSource(FileProviderType providerType);

        bool TryGetSource(FileProviderType providerType, [NotNullWhen(true)] out IFileProviderSource? source);
    }
}
