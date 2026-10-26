using Microsoft.Extensions.DependencyInjection;

namespace Receipts.Sync.Providers.GoogleDrive
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the <see cref="Data.Entities.FileProviderType.GoogleDrive"/> source.
        /// Requires <c>AddFileProviderScanning()</c> to be called as well.
        /// </summary>
        public static IServiceCollection AddGoogleDriveFileProvider(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.AddFileProviderSource<GoogleDriveFileProviderSource>();
        }
    }
}
