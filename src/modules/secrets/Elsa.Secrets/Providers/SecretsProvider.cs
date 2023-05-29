using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Elsa.Secrets.Manager;
using Elsa.Secrets.ValueFormatters;
using Microsoft.Extensions.Logging;

namespace Elsa.Secrets.Providers
{
    public class SecretsProvider : ISecretsProvider
    {
        private readonly ISecretsManager _secretsManager;
        private readonly IEnumerable<ISecretValueFormatter> _valueFormatters;
        private readonly ILogger<SecretsProvider> _logger;

        public SecretsProvider(ISecretsManager secretsManager, IEnumerable<ISecretValueFormatter> valueFormatters, ILogger<SecretsProvider> logger)
        {
            _secretsManager = secretsManager;
            _valueFormatters = valueFormatters;
            _logger = logger;
        }

        public async Task<string?> GetSecretByIdAsync(string id)
        {
            var secret = await _secretsManager.GetSecretById(id);

            var formatter = _valueFormatters.FirstOrDefault(x => x.Type == secret?.Type);

            if (formatter != null)
                return await formatter.FormatSecretValue(secret);

            return null;
        }

        public async Task<string?> GetSecretByNameAsync(string name)
        {
            var secret = await _secretsManager.GetSecretByName(name);

            var formatter = _valueFormatters.FirstOrDefault(x => x.Type == secret?.Type);

            if (formatter != null)
                return await formatter.FormatSecretValue(secret);

            return null;
        }

        public async Task<ICollection<string>> GetSecretsAsync(string type)
        {
            var secrets = await _secretsManager.GetSecrets(type);

            var formatter = _valueFormatters.FirstOrDefault(x => x.Type == type);

            if (formatter != null)
                return secrets.Select(x => formatter.FormatSecretValue(x).Result).ToArray();

            return new List<string>();
        }

        public async Task<string?> GetSecretByNameAsync(string type, string name)
        {
            var secrets = await _secretsManager.GetSecrets(type);

            var formatter = _valueFormatters.FirstOrDefault(x => x.Type == type);

            if (formatter != null)
                return secrets.Where(x => x.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) == true && x.Type?.Equals(type, StringComparison.InvariantCultureIgnoreCase) == true)
                    .Select(x => formatter.FormatSecretValue(x).Result).FirstOrDefault();

            return null;
        }

        public async Task<IDictionary<string, string>> GetSecretsDictionaryAsync(string type)
        {
            var secrets = await _secretsManager.GetSecrets(type);
            return secrets
                .GroupBy(x => $"{type}:{x.Name}").Select(x => x.First())
                .ToDictionary(x => $"{type}:{x.Name}", x => x.Name ?? x.DisplayName ?? x.Id);
        }

        public async Task<IDictionary<string, string>> GetSecretsForSelectListAsync(string type)
            => await GetSecretsDictionaryAsync(type);
    }
}
