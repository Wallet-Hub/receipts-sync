using System.Linq;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Providers.GoogleDrive
{
    /// <summary>
    /// Reads Google credential documents and delegates creation to a factory for their credential type.
    /// </summary>
    public sealed class GoogleDriveCredentialFactory
    {
        private readonly IGoogleDriveCredentialsProvider _credentialsProvider;
        private readonly Dictionary<string, IGoogleDriveCredentialFactory> _credentialFactories;

        public GoogleDriveCredentialFactory(IGoogleDriveCredentialsProvider credentialsProvider, IEnumerable<IGoogleDriveCredentialFactory> credentialFactories)
        {
            ArgumentNullException.ThrowIfNull(credentialsProvider);
            ArgumentNullException.ThrowIfNull(credentialFactories);

            _credentialsProvider = credentialsProvider;
            _credentialFactories = credentialFactories.ToDictionary(factory => factory.CredentialType, StringComparer.Ordinal);
        }

        /// <summary>
        /// Creates a scoped Google Drive credential from the provider's configured credential document.
        /// </summary>
        public async Task<GoogleCredential> CreateAsync(ProviderContext provider, GoogleDriveOptions options, CancellationToken cancellationToken = default)
        {
            await using Stream credentialsStream = await _credentialsProvider
                .OpenReadAsync(provider, options, cancellationToken);

            using StreamReader reader = new(credentialsStream);
            string credentialsJson = await reader.ReadToEndAsync(cancellationToken);
            string? credentialType = ReadCredentialType(credentialsJson, provider);

            if (credentialType is null || !_credentialFactories.TryGetValue(credentialType, out IGoogleDriveCredentialFactory? credentialFactory))
            {
                string supportedTypes = string.Join(
                    " and ",
                    _credentialFactories.Keys.OrderBy(type => type, StringComparer.Ordinal));

                throw new FileProviderConfigurationException(
                    $"Provider '{provider.Name}' has unsupported credential type '{credentialType}'. Supported types are {supportedTypes}.");
            }

            GoogleCredential credential = credentialFactory.Create(credentialsJson)
                .CreateScoped(DriveService.Scope.DriveReadonly);

            if (!string.IsNullOrWhiteSpace(options.ImpersonateUser))
                credential = credential.CreateWithUser(options.ImpersonateUser);

            return credential;
        }

        private static string? ReadCredentialType(string credentialsJson, ProviderContext provider)
        {
            using JsonDocument credentialsDocument = JsonDocument.Parse(credentialsJson);
            JsonElement credentialsRoot = credentialsDocument.RootElement;

            if (credentialsRoot.ValueKind != JsonValueKind.Object
                || !credentialsRoot.TryGetProperty("type", out JsonElement credentialTypeElement)
                || credentialTypeElement.ValueKind != JsonValueKind.String)
            {
                throw new FileProviderConfigurationException(
                    $"The credentials for provider '{provider.Name}' must declare a supported credential type.");
            }

            return credentialTypeElement.GetString();
        }
    }
}
