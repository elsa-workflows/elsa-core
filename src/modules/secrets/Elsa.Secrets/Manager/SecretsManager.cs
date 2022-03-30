using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Elsa.Secrets.Specifications;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Secrets.Manager
{
    public class SecretsManager : ISecretsManager
    {
        private readonly ISecretsStore _secretsStore;
        public SecretsManager(ISecretsStore secretsStore)
        { 
            _secretsStore = secretsStore;
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
