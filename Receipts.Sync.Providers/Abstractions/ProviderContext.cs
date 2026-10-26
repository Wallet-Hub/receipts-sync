using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Providers.Abstractions
{
    /// <summary>
    /// Projection of a <see cref="FileProvider"/> row containing only what a source needs.
    /// Sources depend on this contract instead of the persistence entity, so they never see
    /// navigation properties or database concerns.
    /// </summary>
    /// <param name="Id">Database identity of the provider.</param>
    /// <param name="Name">Human readable name, used in logs and error messages.</param>
    /// <param name="ProviderType">Backend handled by the row.</param>
    /// <param name="Configuration">Provider specific settings as JSON.</param>
    public sealed record ProviderContext(int Id, string Name, FileProviderType ProviderType, string Configuration);
}
