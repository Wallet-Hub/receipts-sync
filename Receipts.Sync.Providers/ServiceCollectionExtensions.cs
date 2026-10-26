using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;
using Receipts.Sync.Providers.Hashing;
using Receipts.Sync.Providers.PhysicalFolder;

namespace Receipts.Sync.Providers
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the provider resolution infrastructure, the hasher and the sources shipped with
        /// this assembly (<see cref="FileProviderType.PhysicalFolder"/>).
        /// </summary>
        public static IServiceCollection AddFileProviderScanning(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IFileHasher, Sha256FileHasher>();
            services.TryAddSingleton<IFileProviderSourceRegistry, FileProviderSourceRegistry>();
            services.AddFileProviderSource<PhysicalFolderFileProviderSource>();

            return services;
        }

        /// <summary>
        /// Registers an additional <see cref="IFileProviderSource"/> implementation. Registering the
        /// same implementation twice is a no-op, so feature assemblies can be added freely.
        /// </summary>
        public static IServiceCollection AddFileProviderSource<TSource>(this IServiceCollection services) where TSource : class, IFileProviderSource
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileProviderSource, TSource>());
            return services;
        }
    }
}
