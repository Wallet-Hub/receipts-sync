using System.Text;
using Receipts.Sync.Providers.Hashing;

namespace Receipts.Sync.Tests.Providers
{
    public sealed class Sha256FileHasherTests
    {
        private readonly Sha256FileHasher _hasher = new();

        [Fact]
        public void AlgorithmName_IsSha256()
        {
            Assert.Equal("SHA256", _hasher.AlgorithmName);
        }

        [Fact]
        public async Task ComputeHashAsync_ReturnsLowercaseHexDigest()
        {
            await using var content = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

            string hash = await _hasher.ComputeHashAsync(content);

            Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
        }

        [Fact]
        public async Task ComputeHashAsync_ReturnsSameHashForSameContent()
        {
            await using var first = new MemoryStream(Encoding.UTF8.GetBytes("receipt"));
            await using var second = new MemoryStream(Encoding.UTF8.GetBytes("receipt"));

            Assert.Equal(
                await _hasher.ComputeHashAsync(first),
                await _hasher.ComputeHashAsync(second));
        }

        [Fact]
        public async Task ComputeHashAsync_ReturnsDifferentHashWhenContentChanges()
        {
            await using var first = new MemoryStream(Encoding.UTF8.GetBytes("receipt-2025"));
            await using var second = new MemoryStream(Encoding.UTF8.GetBytes("receipt-2026"));

            Assert.NotEqual(
                await _hasher.ComputeHashAsync(first),
                await _hasher.ComputeHashAsync(second));
        }
    }
}
