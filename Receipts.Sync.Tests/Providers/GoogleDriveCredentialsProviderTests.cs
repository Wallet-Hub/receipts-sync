using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;
using Receipts.Sync.Providers.GoogleDrive;

namespace Receipts.Sync.Tests.Providers
{
    public sealed class GoogleDriveCredentialsProviderTests
    {
        [Fact]
        public async Task OpenReadAsync_ReturnsStreamForConfiguredCredentialsFile()
        {
            string credentialsPath = Path.GetTempFileName();

            try
            {
                const string credentialsJson = "{\"type\":\"service_account\"}";
                await File.WriteAllTextAsync(credentialsPath, credentialsJson);

                ProviderContext provider = new(1, "drive", FileProviderType.GoogleDrive, "{}");
                GoogleDriveOptions options = new() { CredentialsPath = credentialsPath };
                IGoogleDriveCredentialsProvider credentialsProvider = new FileGoogleDriveCredentialsProvider();

                await using Stream stream = await credentialsProvider.OpenReadAsync(provider, options);
                using StreamReader reader = new(stream);
                string actual = await reader.ReadToEndAsync();

                Assert.Equal(credentialsJson, actual);
            }
            finally
            {
                File.Delete(credentialsPath);
            }
        }

        [Fact]
        public async Task OpenReadAsync_ThrowsConfigurationExceptionWhenPathIsMissing()
        {
            ProviderContext provider = new(1, "drive", FileProviderType.GoogleDrive, "{}");
            GoogleDriveOptions options = new();
            IGoogleDriveCredentialsProvider credentialsProvider = new FileGoogleDriveCredentialsProvider();

            await Assert.ThrowsAsync<FileProviderConfigurationException>(
                () => credentialsProvider.OpenReadAsync(provider, options));
        }
    }
}
