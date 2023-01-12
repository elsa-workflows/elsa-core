using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Elsa.Secrets.Specifications;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Persistence.Specifications;
using System.Linq;

namespace Elsa.Secrets.Manager
{
    public class SecretsManager : ISecretsManager
    {
        private readonly ISecretsStore _secretsStore;
        public SecretsManager(ISecretsStore secretsStore)
        { 
            _secretsStore = secretsStore;
        }

        public async Task<Secret?> GetSecretById(string id, CancellationToken cancellationToken = default) {
            var specification = new SecretsIdSpecification(id);
            var secret = await _secretsStore.FindAsync(specification, cancellationToken: cancellationToken);

            return secret;
        }

        public async Task<Secret?> GetSecretByName(string name, CancellationToken cancellationToken = default) {
            var specification = new SecretsNameSpecification(name);
            var secret = await _secretsStore.FindManyAsync(specification, OrderBySpecification.OrderBy<Secret>(s => s.Type), cancellationToken: cancellationToken);

            return secret.FirstOrDefault(); ;
        }

        public async Task<IEnumerable<Secret>> GetSecrets(CancellationToken cancellationToken = default)
        {
            var specification = Specification<Secret>.Identity;
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            return secrets;
        }

        public async Task<IEnumerable<Secret>> GetSecrets(string type, CancellationToken cancellationToken = default)
        {
            var specification = new SecretTypeSpecification(type);
            var secrets = await _secretsStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            return secrets;
        }
    }
}
