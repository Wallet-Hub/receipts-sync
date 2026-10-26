using System.Diagnostics.CodeAnalysis;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Providers
{
    /// <summary>
    /// Dictionary based <see cref="IFileProviderSourceRegistry"/>: the available sources are injected
    /// by the DI container, so registering a new provider is enough to make it usable.
    /// </summary>
    public sealed class FileProviderSourceRegistry : IFileProviderSourceRegistry
    {
        private readonly IReadOnlyDictionary<FileProviderType, IFileProviderSource> _sources;

        public FileProviderSourceRegistry(IEnumerable<IFileProviderSource> sources)
        {
            ArgumentNullException.ThrowIfNull(sources);

            var map = new Dictionary<FileProviderType, IFileProviderSource>();

            foreach (IFileProviderSource source in sources)
            {
                if (!map.TryAdd(source.ProviderType, source))
                {
                    throw new InvalidOperationException($"Multiple file provider sources are registered for '{source.ProviderType}'. A provider type must be handled by exactly one source.");
                }
            }

            _sources = map;
        }

        public IFileProviderSource GetSource(FileProviderType providerType)
        {
            return TryGetSource(providerType, out IFileProviderSource? source)
                ? source
                : throw new NotSupportedException($"No file provider source is registered for '{providerType}'.");
        }

        public bool TryGetSource(FileProviderType providerType, [NotNullWhen(true)] out IFileProviderSource? source)
        {
            return _sources.TryGetValue(providerType, out source);
        }
    }
}
