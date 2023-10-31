namespace Elsa.Secrets.Encryption
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Elsa.Secrets.Models;
    using Elsa.Secrets.Options;
    using Microsoft.Extensions.Options;

    public class AesSecretEncryptor : ISecretEncryptor
    {
        private readonly SecretsConfigOptions _secretsConfig;

        public AesSecretEncryptor(IOptions<SecretsConfigOptions> options)
        {
            _secretsConfig = options.Value;
        }

        public Task EncryptProperties(Secret secret, CancellationToken cancellationToken = default)
        {
            if (_secretsConfig.EncryptionEnabled == true)
            {
                foreach (var property in secret.Properties)
                {
                    if (property.IsEncrypted || (_secretsConfig.EncryptedProperties?.Contains(property.Name, StringComparer.OrdinalIgnoreCase) ?? false))
                    {
                        continue;
                    }

                    foreach (var (key, value) in property.Expressions)
                    {
                        property.Expressions[key] = AesEncryption.Encrypt(_secretsConfig.EncryptionKey, value);
                    }

                    property.IsEncrypted = true;
                }
            }

            return Task.CompletedTask;
        }

        public Task DecryptPropertiesAsync(Secret secret, CancellationToken cancellationToken = default)
        {
            if (_secretsConfig.EncryptionEnabled == true)
            {
                foreach (var property in secret.Properties)
                {
                    if (!property.IsEncrypted)
                    {
                        continue;
                    }

                    foreach (var (key, value) in property.Expressions)
                    {
                        property.Expressions[key] = AesEncryption.Decrypt(_secretsConfig.EncryptionKey, value);
                    }

                    property.IsEncrypted = false;
                }
            }

            return Task.CompletedTask;
        }
    }
}
