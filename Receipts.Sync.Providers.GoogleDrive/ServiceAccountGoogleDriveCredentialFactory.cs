using Google.Apis.Auth.OAuth2;

namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// Creates credentials from Google service-account JSON documents.
    /// </summary>
    public sealed class ServiceAccountGoogleDriveCredentialFactory : IGoogleDriveCredentialFactory
    {
        /// <inheritdoc />
        public string CredentialType => "service_account";

        /// <inheritdoc />
        public GoogleCredential Create(string credentialsJson)
        {
            ServiceAccountCredential credential = CredentialFactory.FromJson<ServiceAccountCredential>(credentialsJson);
            return credential.ToGoogleCredential();
        }
    }
}
