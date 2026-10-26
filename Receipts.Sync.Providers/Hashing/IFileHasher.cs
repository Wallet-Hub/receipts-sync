namespace Receipts.Sync.Providers.Hashing
{
    /// <summary>
    /// Computes the content hash recorded for every scanned receipt file.
    /// </summary>
    public interface IFileHasher
    {
        /// <summary>
        /// Name of the algorithm, persisted together with the hash.
        /// </summary>
        string AlgorithmName { get; }

        /// <summary>
        /// Computes the hash of <paramref name="content"/> as a lowercase hexadecimal string.
        /// The stream is read from its current position but not disposed.
        /// </summary>
        Task<string> ComputeHashAsync(Stream content, CancellationToken cancellationToken = default);
    }
}
