using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Secrets.Extentions;
using Elsa.Secrets.Manager;

namespace Elsa.Secrets.Providers
{
    public class SecretsProvider : ISecretsProvider
    {
        private readonly ISecretsManager _secretsManager;
        public SecretsProvider(ISecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        public async Task<ICollection<string>> GetSecrets(string type)
        {
            var secrets = await _secretsManager.GetSecrets(type);

            return secrets.Select(x => x.GetValue()).ToArray();
        }

        public async Task<ICollection<(string, string)>> GetSecretsForSelectListAsync(string type)
        {
            var secrets = await _secretsManager.GetSecrets(type);
            return secrets.Select(x => (x.Name ?? x.DisplayName, x.GetValue())).ToArray();
        }
    }
}
