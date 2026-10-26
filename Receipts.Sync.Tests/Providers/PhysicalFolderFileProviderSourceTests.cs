using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;
using Receipts.Sync.Providers.PhysicalFolder;

namespace Receipts.Sync.Tests.Providers
{
    public sealed class PhysicalFolderFileProviderSourceTests : IDisposable
    {
        private readonly string _root;
        private readonly PhysicalFolderFileProviderSource _source = new(NullLogger<PhysicalFolderFileProviderSource>.Instance);

        public PhysicalFolderFileProviderSourceTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "receipts-sync-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "2025"));
            File.WriteAllText(Path.Combine(_root, "invoice.pdf"), "pdf-content");
            File.WriteAllText(Path.Combine(_root, "notes.txt"), "ignored");
            File.WriteAllText(Path.Combine(_root, "2025", "march.pdf"), "march-content");
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Fact]
        public async Task EnumerateFilesAsync_ReturnsRelativeIdsUsingForwardSlashes()
        {
            List<ProviderFileInfo> files = await CollectAsync(CreateProvider(recursive: true));

            Assert.Equal(["2025/march.pdf", "invoice.pdf", "notes.txt"], files.Select(f => f.ExternalId).Order());
            Assert.Equal("pdf-content".Length, files.Single(f => f.ExternalId == "invoice.pdf").SizeInBytes);
        }

        [Fact]
        public async Task EnumerateFilesAsync_RespectsRecursiveFlag()
        {
            List<ProviderFileInfo> files = await CollectAsync(CreateProvider(recursive: false));

            Assert.DoesNotContain(files, f => f.ExternalId.Contains('/'));
            Assert.Equal(2, files.Count);
        }

        [Fact]
        public async Task EnumerateFilesAsync_AppliesExtensionFilter()
        {
            List<ProviderFileInfo> files = await CollectAsync(CreateProvider(recursive: true, extensions: [".pdf"]));

            Assert.Equal(["2025/march.pdf", "invoice.pdf"], files.Select(f => f.ExternalId).Order());
        }

        [Fact]
        public async Task EnumerateFilesAsync_SetsContentTypeAndTimestamps()
        {
            List<ProviderFileInfo> files = await CollectAsync(CreateProvider(recursive: true, extensions: [".pdf"]));
            ProviderFileInfo file = files.Single(f => f.ExternalId == "invoice.pdf");

            Assert.Equal("invoice.pdf", file.FileName);
            Assert.Equal("application/pdf", file.ContentType);
            Assert.NotNull(file.CreatedAt);
            Assert.NotNull(file.UpdatedAt);
            Assert.Null(file.Url);
        }

        [Fact]
        public async Task OpenReadAsync_ReturnsFileContent()
        {
            ProviderContext provider = CreateProvider(recursive: true);
            ProviderFileInfo file = (await CollectAsync(provider)).Single(f => f.ExternalId == "invoice.pdf");

            await using Stream content = await _source.OpenReadAsync(provider, file);
            using var reader = new StreamReader(content);

            Assert.Equal("pdf-content", await reader.ReadToEndAsync());
        }

        [Fact]
        public async Task OpenReadAsync_RejectsPathsOutsideTheRootFolder()
        {
            ProviderContext provider = CreateProvider(recursive: true);
            var escaping = new ProviderFileInfo { ExternalId = "../outside.txt" };

            await Assert.ThrowsAsync<FileProviderConfigurationException>(
                () => _source.OpenReadAsync(provider, escaping));
        }

        [Fact]
        public async Task EnumerateFilesAsync_ThrowsWhenRootFolderIsMissing()
        {
            var provider = new ProviderContext(
                1,
                "missing",
                FileProviderType.PhysicalFolder,
                JsonSerializer.Serialize(new PhysicalFolderOptions
                {
                    RootPath = Path.Combine(_root, "does-not-exist"),
                }));

            await Assert.ThrowsAsync<FileProviderConfigurationException>(async () =>
            {
                await foreach (var _ in _source.EnumerateFilesAsync(provider))
                {
                }
            });
        }

        private ProviderContext CreateProvider(bool recursive, string[]? extensions = null)
        {
            return new ProviderContext(
                1,
                "local",
                FileProviderType.PhysicalFolder,
                JsonSerializer.Serialize(new PhysicalFolderOptions
                {
                    RootPath = _root,
                    Recursive = recursive,
                    Extensions = extensions,
                }));
        }

        private async Task<List<ProviderFileInfo>> CollectAsync(ProviderContext provider)
        {
            var files = new List<ProviderFileInfo>();

            await foreach (var file in _source.EnumerateFilesAsync(provider))
            {
                files.Add(file);
            }

            return files;
        }
    }
}
