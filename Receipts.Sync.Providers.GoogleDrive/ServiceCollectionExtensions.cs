using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

            services.TryAddSingleton<IGoogleDriveCredentialsProvider, FileGoogleDriveCredentialsProvider>();
            return services.AddFileProviderSource<GoogleDriveFileProviderSource>();
        }
    }
}
