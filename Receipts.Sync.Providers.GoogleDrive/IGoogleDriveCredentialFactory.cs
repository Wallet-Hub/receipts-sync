using Google.Apis.Auth.OAuth2;

namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// Creates a Google credential from a supported credential JSON document type.
    /// </summary>
    public interface IGoogleDriveCredentialFactory
    {
        /// <summary>
        /// The value of the JSON document's <c>type</c> property handled by this factory.
        /// </summary>
        string CredentialType { get; }

        /// <summary>
        /// Creates a Google credential from its JSON document.
        /// </summary>
        GoogleCredential Create(string credentialsJson);
    }
}
