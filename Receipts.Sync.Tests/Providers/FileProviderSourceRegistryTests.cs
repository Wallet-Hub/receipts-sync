using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers;
using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Tests.Providers
{
    public sealed class FileProviderSourceRegistryTests
    {
        [Fact]
        public void GetSource_ReturnsRegisteredSource()
        {
            var source = new StubSource(FileProviderType.PhysicalFolder);
            var registry = new FileProviderSourceRegistry([source]);

            Assert.Same(source, registry.GetSource(FileProviderType.PhysicalFolder));
        }

        [Fact]
        public void TryGetSource_ReturnsFalseForUnregisteredProviderType()
        {
            var registry = new FileProviderSourceRegistry([new StubSource(FileProviderType.PhysicalFolder)]);

            Assert.False(registry.TryGetSource(FileProviderType.GoogleDrive, out IFileProviderSource? source));
            Assert.Null(source);
        }

        [Fact]
        public void GetSource_ThrowsForUnregisteredProviderType()
        {
            var registry = new FileProviderSourceRegistry([new StubSource(FileProviderType.PhysicalFolder)]);

            Assert.Throws<NotSupportedException>(() => registry.GetSource(FileProviderType.AzureBlobStorage));
        }

        [Fact]
        public void Constructor_ThrowsWhenTwoSourcesHandleTheSameProviderType()
        {
            var sources = new[]
            {
                new StubSource(FileProviderType.GoogleDrive),
                new StubSource(FileProviderType.GoogleDrive),
            };

            Assert.Throws<InvalidOperationException>(() => new FileProviderSourceRegistry(sources));
        }

        private sealed class StubSource : IFileProviderSource
        {
            public StubSource(FileProviderType providerType)
            {
                ProviderType = providerType;
            }

            public FileProviderType ProviderType { get; }

            public IAsyncEnumerable<ProviderFileInfo> EnumerateFilesAsync(
                ProviderContext provider,
                CancellationToken cancellationToken = default) => Empty();

            public Task<Stream> OpenReadAsync(
                ProviderContext provider,
                ProviderFileInfo file,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            private static async IAsyncEnumerable<ProviderFileInfo> Empty()
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }
}
