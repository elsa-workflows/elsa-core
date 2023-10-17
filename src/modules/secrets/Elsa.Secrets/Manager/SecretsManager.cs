using System;
using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Elsa.Secrets.Specifications;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Persistence.Specifications;
using System.Linq;
using Elsa.Secrets.Encryption;
using Elsa.Secrets.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Manager
{
    public class SecretsManager : ISecretsManager
    {
        private readonly ISecretsStore _secretsStore;
        private readonly bool _encryptionEnabled;
        private readonly string _encryptionKey;
        private readonly string[] _encryptedProperties;

        public SecretsManager(ISecretsStore secretsStore, IOptions<SecretsConfigOptions> options)
        {
            _secretsStore = secretsStore;
            
            _encryptionEnabled = options.Value.Enabled ?? false;
            _encryptionKey = options.Value.EncryptionKey;
            _encryptedProperties = options.Value.EncryptedProperties;
        }

        public async Task<Secret?> GetSecretById(string id, CancellationToken cancellationToken = default) {
            var specification = new SecretsIdSpecification(id);
            var secret = await _secretsStore.FindAsync(specification, cancellationToken: cancellationToken);
            DecryptProperties(secret);

            return secret;
        }

        public async Task<Secret?> GetSecretByName(string name, CancellationToken cancellationToken = default) {
            var specification = new SecretsNameSpecification(name);
            var secrets = await _secretsStore.FindManyAsync(specification, OrderBySpecification.OrderBy<Secret>(s => s.Type), cancellationToken: cancellationToken);
            var secret = secrets.FirstOrDefault();
            DecryptProperties(secret);
            
            return secret;
        }

        public async Task<IEnumerable<Secret>> GetSecrets(CancellationToken cancellationToken = default)
        {
            var specification = Specification<Secret>.Identity;
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            foreach (var secret in secrets)
            {
                DecryptProperties(secret);
            }

            return secrets;
        }
        
        public async Task<IEnumerable<Secret>> GetSecretViewModels(CancellationToken cancellationToken = default)
        {
            var specification = Specification<Secret>.Identity;
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            foreach (var secret in secrets)
            {
                HideEncryptedProperties(secret);
            }

            return secrets;
        }

        public async Task<IEnumerable<Secret>> GetSecrets(string type, bool decrypt = true, CancellationToken cancellationToken = default)
        {
            var specification = new SecretTypeSpecification(type);
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            
            if (decrypt)
            {
                foreach (var secret in secrets)
                {
                    DecryptProperties(secret);
                }
            }
            

            return secrets;
        }

        public async Task<Secret> AddOrUpdateSecret(Secret secret, bool restoreHiddenProperties, CancellationToken cancellationToken = default)
        {
            var clone = secret.Clone() as Secret;

            if (restoreHiddenProperties)
            {
                await RestoreHiddenProperties(clone, cancellationToken);
            }
            EncryptProperties(clone);
            
            if (clone.Id == null)
                await _secretsStore.AddAsync(clone);
            else
                await _secretsStore.UpdateAsync(clone);
            return clone;
        }
        
        private async Task RestoreHiddenProperties(Secret secret, CancellationToken cancellationToken)
        {
            var specification = new SecretsIdSpecification(secret.Id);
            var existingSecret = await _secretsStore.FindAsync(specification, cancellationToken: cancellationToken);
            if (existingSecret != null)
            {
                foreach (var property in secret.Properties)
                {
                    if (property.IsEncrypted)
                    {
                        property.Expressions = existingSecret.Properties.First(x => x.Name == property.Name).Expressions;
                    }
                }
            }
        }
        
        private void HideEncryptedProperties(Secret secret)
        {
            foreach (var secretProperty in secret.Properties)
            {
                if (!secretProperty.IsEncrypted) continue;
                foreach (var key in secretProperty.Expressions.Keys)
                {
                    secretProperty.Expressions[key] = new string('*', 8);
                }
            }
        }
        
        private void EncryptProperties(Secret secret)
        {
            if (!_encryptionEnabled)
            {
                return;
            }
            foreach (var property in secret.Properties)
            {
                var encrypt = _encryptedProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase);
                if (!encrypt || property.IsEncrypted)
                {
                    continue;
                }

                foreach (var key in property.Expressions.Keys)
                {
                    var value = property.Expressions[key];
                    property.Expressions[key] = AesEncryption.Encrypt(_encryptionKey, value);
                }

                property.IsEncrypted = true;
            }
        }
    
        private void DecryptProperties(Secret secret)
        {
            if (!_encryptionEnabled)
            {
                return;
            }
            foreach (var property in secret.Properties)
            {
                if (!property.IsEncrypted)
                {
                    continue;
                }

                foreach (var key in property.Expressions.Keys)
                {
                    var value = property.Expressions[key];
                    property.Expressions[key] = AesEncryption.Decrypt(_encryptionKey, value);
                }

                property.IsEncrypted = false;
            }
        }
    }
}
