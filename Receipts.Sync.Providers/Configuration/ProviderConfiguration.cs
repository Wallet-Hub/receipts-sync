using System.Text.Json;
using Receipts.Sync.Providers.Abstractions;

namespace Receipts.Sync.Providers.Configuration
{
    /// <summary>
    /// Reads the provider-specific settings stored in <see cref="ProviderContext.Configuration"/>.
    /// </summary>
    public static class ProviderConfiguration
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// Deserializes the JSON configuration of <paramref name="provider"/> into <typeparamref name="T"/>.
        /// An empty configuration is treated as an empty object.
        /// </summary>
        /// <exception cref="FileProviderConfigurationException">The configuration is not valid JSON.</exception>
        public static T Read<T>(ProviderContext provider) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(provider);

            if (string.IsNullOrWhiteSpace(provider.Configuration))
            {
                return new T();
            }

            try
            {
                return JsonSerializer.Deserialize<T>(provider.Configuration, SerializerOptions)
                    ?? throw new FileProviderConfigurationException($"The configuration of provider '{provider.Name}' is empty.");
            }
            catch (JsonException ex)
            {
                throw new FileProviderConfigurationException($"The configuration of provider '{provider.Name}' ({provider.ProviderType}) is not valid JSON: {ex.Message}", ex);
            }
        }
    }
}
