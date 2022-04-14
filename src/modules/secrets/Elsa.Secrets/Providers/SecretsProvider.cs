using Elsa.Secrets.Manager;
using Elsa.Secrets.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Providers
{
    public class SecretsProvider : ISecretsProvider
    {
        private readonly ISecretsManager _secretsManager;
        public SecretsProvider(ISecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        public async Task<ICollection<string>> GetSecrets(string type, string separator)
        {
            var secrets = await _secretsManager.GetSecrets(type);

            return secrets.Select(x => convertToCredentialString(x.PropertiesJson, separator)).ToArray();
        }

        private string convertToCredentialString(string propertiesJson, string separator)
        {
            var properties = JsonConvert.DeserializeObject(propertiesJson).ConvertTo<ICollection<SecretProperty>>();
            var sb = new StringBuilder();

            foreach (var property in properties)
            {
                if (property.Expressions.Count() > 0)
                    sb.Append($"{property.Name}{separator}{property.Expressions.First().Value};");
            }

            return sb.ToString();
        }
    }
}
