using System.Runtime.CompilerServices;
using System.Text;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Tests.Sync
{
    /// <summary>
    /// In-memory <see cref="IFileProviderSource"/> used to drive <c>ReceiptSyncService</c> without touching
    /// a real storage. It also counts how many times a file content was opened, to assert hash reuse.
    /// </summary>
    internal sealed class FakeFileProviderSource : IFileProviderSource
    {
        private readonly List<FakeFile> _files = [];

        public FakeFileProviderSource(FileProviderType providerType = FileProviderType.PhysicalFolder)
        {
            ProviderType = providerType;
        }

        public FileProviderType ProviderType { get; }

        public int OpenReadCount { get; private set; }

        public FakeFileProviderSource Add(
            string externalId,
            string content,
            DateTime? createdAt = null,
            DateTime? updatedAt = null)
        {
            _files.RemoveAll(f => f.Info.ExternalId == externalId);
            _files.Add(new FakeFile(
                new ProviderFileInfo
                {
                    ExternalId = externalId,
                    FileName = externalId[(externalId.LastIndexOf('/') + 1)..],
                    ContentType = "application/pdf",
                    SizeInBytes = Encoding.UTF8.GetByteCount(content),
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                },
                Encoding.UTF8.GetBytes(content)));

            return this;
        }

        public async IAsyncEnumerable<ProviderFileInfo> EnumerateFilesAsync(
            ProviderContext provider,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var file in _files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return file.Info;
            }

            await Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(
            ProviderContext provider,
            ProviderFileInfo file,
            CancellationToken cancellationToken = default)
        {
            FakeFile match = _files.SingleOrDefault(f => f.Info.ExternalId == file.ExternalId)
                ?? throw new FileNotFoundException($"Unknown file '{file.ExternalId}'.", file.ExternalId);

            OpenReadCount++;
            return Task.FromResult<Stream>(new MemoryStream(match.Content, writable: false));
        }

        private sealed record FakeFile(ProviderFileInfo Info, byte[] Content);
    }
}
