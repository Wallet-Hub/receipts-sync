using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Core.Sync
{
    /// <summary>
    /// Projects the persistence entity into the provider contract, keeping the mapping in the layer
    /// that already knows both models.
    /// </summary>
    internal static class ProviderContextExtensions
    {
        public static ProviderContext ToProviderContext(this FileProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            return new ProviderContext(provider.Id, provider.Name, provider.ProviderType, provider.Configuration);
        }
    }
}
