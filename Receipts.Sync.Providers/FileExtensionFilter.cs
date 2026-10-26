namespace Receipts.Sync.Providers
{
    /// <summary>
    /// Applies the optional <c>extensions</c> setting shared by the provider configurations.
    /// </summary>
    public static class FileExtensionFilter
    {
        /// <summary>
        /// Returns <c>true</c> when <paramref name="fileName"/> matches one of <paramref name="extensions"/>.
        /// An empty or missing list accepts every file.
        /// </summary>
        public static bool Matches(string? fileName, IReadOnlyList<string>? extensions)
        {
            if (extensions is null || extensions.Count == 0)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            foreach (string? extension in extensions)
            {
                if (string.IsNullOrWhiteSpace(extension))
                {
                    continue;
                }

                string? normalized = extension[0] == '.' ? extension : "." + extension;
                if (fileName.EndsWith(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
