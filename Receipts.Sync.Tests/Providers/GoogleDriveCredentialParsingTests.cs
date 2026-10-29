using System.Security.Cryptography;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace Receipts.Sync.Tests.Providers
{
    public sealed class GoogleDriveCredentialParsingTests
    {
        [Fact]
        public void ServiceAccountJson_CreatesGoogleCredential()
        {
            using RSA rsa = RSA.Create(2048);
            Dictionary<string, string> credentials = new()
            {
                ["type"] = "service_account",
                ["project_id"] = "test-project",
                ["private_key_id"] = "test-key",
                ["private_key"] = rsa.ExportPkcs8PrivateKeyPem(),
                ["client_email"] = "receipts-sync@test-project.iam.gserviceaccount.com",
                ["client_id"] = "123456789",
                ["token_uri"] = "https://oauth2.googleapis.com/token",
            };
            string credentialsJson = JsonSerializer.Serialize(credentials);

            ServiceAccountCredential serviceAccountCredential = CredentialFactory
                .FromJson<ServiceAccountCredential>(credentialsJson);
            GoogleCredential credential = serviceAccountCredential.ToGoogleCredential();

            Assert.NotNull(credential);
        }
    }
}
