using Google.Apis.Auth.OAuth2;

namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// Creates credentials from Google authorized-user JSON documents.
    /// </summary>
    public sealed class AuthorizedUserGoogleDriveCredentialFactory : IGoogleDriveCredentialFactory
    {
        /// <inheritdoc />
        public string CredentialType => "authorized_user";

        /// <inheritdoc />
        public GoogleCredential Create(string credentialsJson)
        {
            UserCredential credential = CredentialFactory.FromJson<UserCredential>(credentialsJson);
            return credential.ToGoogleCredential();
        }
    }
}
