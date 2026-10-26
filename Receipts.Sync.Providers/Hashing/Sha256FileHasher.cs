using System.Security.Cryptography;

namespace Receipts.Sync.Providers.Hashing
{
    /// <summary>
    /// Default <see cref="IFileHasher"/>: streams the content through SHA-256 without buffering it in memory.
    /// </summary>
    public sealed class Sha256FileHasher : IFileHasher
    {
        public string AlgorithmName => "SHA256";

        public async Task<string> ComputeHashAsync(Stream content, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            byte[] hash = await SHA256.HashDataAsync(content, cancellationToken).ConfigureAwait(false);

            return Convert.ToHexStringLower(hash);
        }
    }
}
