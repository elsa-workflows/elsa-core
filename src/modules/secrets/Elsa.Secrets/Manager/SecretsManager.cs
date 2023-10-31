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

namespace Elsa.Secrets.Manager
{
    public class SecretsManager : ISecretsManager
    {
        private static readonly string HidedValue = new string('*', 8);
        private readonly ISecretsStore _secretsStore;
        private readonly ISecretEncryptor _encryptor;

        public SecretsManager(ISecretsStore secretsStore, ISecretEncryptor encryptor)
        {
            _secretsStore = secretsStore;
            _encryptor = encryptor;
        }

        public virtual async Task<Secret?> GetSecretById(string id, CancellationToken cancellationToken = default) {
            var specification = new SecretsIdSpecification(id);
            var secret = await _secretsStore.FindAsync(specification, cancellationToken: cancellationToken);
            await _encryptor.DecryptPropertiesAsync(secret, cancellationToken);

            return secret;
        }

        public virtual async Task<Secret?> GetSecretByName(string name, CancellationToken cancellationToken = default) {
            var specification = new SecretsNameSpecification(name);
            var secrets = await _secretsStore.FindManyAsync(specification, OrderBySpecification.OrderBy<Secret>(s => s.Type), cancellationToken: cancellationToken);
            var secret = secrets.FirstOrDefault();
            await _encryptor.DecryptPropertiesAsync(secret, cancellationToken);
            
            return secret;
        }

        public virtual async Task<IEnumerable<Secret>> GetSecrets(CancellationToken cancellationToken = default)
        {
            var specification = Specification<Secret>.Identity;
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            foreach (var secret in secrets)
            {
                await _encryptor.DecryptPropertiesAsync(secret, cancellationToken);
            }

            return secrets;
        }
        
        public virtual async Task<IEnumerable<Secret>> GetSecretViewModels(CancellationToken cancellationToken = default)
        {
            var specification = Specification<Secret>.Identity;
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            foreach (var secret in secrets)
            {
                HideEncryptedProperties(secret);
            }

            return secrets;
        }

        public virtual async Task<IEnumerable<Secret>> GetSecrets(string type, bool decrypt = true, CancellationToken cancellationToken = default)
        {
            var specification = new SecretTypeSpecification(type);
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            
            if (decrypt)
            {
                foreach (var secret in secrets)
                {
                    await _encryptor.DecryptPropertiesAsync(secret, cancellationToken);
                }
            }
            
            return secrets;
        }

        public virtual async Task<Secret> AddOrUpdateSecret(Secret secret, bool restoreHiddenProperties, CancellationToken cancellationToken = default)
        {
            var clone = secret.Clone() as Secret;

            if (restoreHiddenProperties)
            {
                await RestoreHiddenProperties(clone, cancellationToken);
            }
            
            await _encryptor.EncryptProperties(clone, cancellationToken);
            
            if (clone.Id == null)
                await _secretsStore.AddAsync(clone, cancellationToken);
            else
                await _secretsStore.UpdateAsync(clone, cancellationToken);
            return clone;
        }
        
        protected virtual async Task RestoreHiddenProperties(Secret secret, CancellationToken cancellationToken)
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

        protected virtual void HideEncryptedProperties(Secret secret)
        {
            foreach (var secretProperty in secret.Properties)
            {
                if (!secretProperty.IsEncrypted) continue;
                foreach (var key in secretProperty.Expressions.Keys)
                {
                    secretProperty.Expressions[key] = HidedValue;
                }
            }
        }
    }
}
