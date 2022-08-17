using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Secrets.Manager;
using Elsa.Secrets.ValueFormatters;

namespace Elsa.Secrets.Providers
{
    public class SecretsProvider : ISecretsProvider
    {
        private readonly ISecretsManager _secretsManager;
        private readonly IEnumerable<ISecretValueFormatter> _valueFormatters;
        public SecretsProvider(ISecretsManager secretsManager, IEnumerable<ISecretValueFormatter> valueFormatters)
        {
            _secretsManager = secretsManager;
            _valueFormatters = valueFormatters;
        }

        public async Task<ICollection<string>> GetSecrets(string type)
        {
            var secrets = await _secretsManager.GetSecrets(type);

            var formatter = _valueFormatters.FirstOrDefault(x => x.Type == type);

            if (formatter != null)
                return secrets.Select(x => formatter.FormatSecretValue(x)).ToArray();

            return new List<string>();
        }

        public async Task<ICollection<(string, string)>> GetSecretsForSelectListAsync(string type)
        {
            var secrets = await _secretsManager.GetSecrets(type);

            var formatter = _valueFormatters.FirstOrDefault(x => x.Type == type);

            if (formatter != null)
                return secrets.Select(x => (x.Name ?? x.DisplayName, formatter.FormatSecretValue(x))).ToArray();

            return new List<(string, string)>();
        }
    }
}
