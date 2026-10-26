namespace Receipts.Sync.Providers
{
    /// <summary>
    /// Minimal file-extension to MIME type mapping, used by providers that do not report a content type.
    /// </summary>
    public static class ContentTypes
    {
        private static readonly Dictionary<string, string> ByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".webp"] = "image/webp",
            [".heic"] = "image/heic",
            [".heif"] = "image/heif",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".xml"] = "application/xml",
            [".json"] = "application/json",
            [".eml"] = "message/rfc822",
        };

        /// <summary>
        /// Returns the MIME type matching the extension of <paramref name="fileName"/>,
        /// or <c>null</c> when the extension is unknown.
        /// </summary>
        public static string? FromFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            string? extension = Path.GetExtension(fileName);
            return ByExtension.TryGetValue(extension, out string? contentType) ? contentType : null;
        }
    }
}
