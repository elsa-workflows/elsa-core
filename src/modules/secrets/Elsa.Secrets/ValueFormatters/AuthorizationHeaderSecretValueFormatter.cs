using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Secrets.Models;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.ValueFormatters
{
    public class AuthorizationHeaderSecretValueFormatter : ISecretValueFormatter
    {
        private readonly ISecuredSecretService _securedSecretService;

        public AuthorizationHeaderSecretValueFormatter(ISecuredSecretService securedSecretService)
        {
            _securedSecretService = securedSecretService;
        }

        public string Type => "Authorization";

        public Task<string> FormatSecretValue(Secret secret)
        {
            _securedSecretService.SetSecret(secret);
            return Task.FromResult(ConvertPropertiesToString(_securedSecretService.GetAllProperties()));
        }

        private static string ConvertPropertiesToString(IEnumerable<SecretProperty> properties)
        {
            var sb = new StringBuilder();

            foreach (var property in properties.Where(x => x.Expressions.Count > 0))
            {
                sb.Append($"{property.Expressions.First().Value}");
            }

            return sb.ToString();
        }
    }
}
