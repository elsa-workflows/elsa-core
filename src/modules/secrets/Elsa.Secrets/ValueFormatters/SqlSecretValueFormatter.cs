using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.ValueFormatters
{
    public abstract class SqlSecretValueFormatter : ISecretValueFormatter
    {
        public abstract string Type { get; }

        public virtual string KeyValueSeparator => "=";

        public virtual string SettingSeparator => ";";

        public Task<string> FormatSecretValue(Secret secret) => Task.FromResult(ConvertPropertiesToString(secret.Properties, KeyValueSeparator, SettingSeparator));

        private static string ConvertPropertiesToString(ICollection<SecretProperty> properties, string keyValueSeparator, string settingSeparator)
        {
            var sb = new StringBuilder();
            
            foreach (var property in properties.Where(x => x.Name.Equals("AdditionalSettings", System.StringComparison.OrdinalIgnoreCase) == false && x.Expressions.Count > 0))
                if (string.IsNullOrWhiteSpace(property.Expressions.First().Value) == false)
                    sb.Append($"{property.Name}{keyValueSeparator}{property.Expressions.First().Value}{settingSeparator}");

            var additionalSettings = properties.Where(x => x.Name.Equals("AdditionalSettings", System.StringComparison.OrdinalIgnoreCase) == true && x.Expressions.Count > 0).FirstOrDefault();
            if (additionalSettings is not null && string.IsNullOrWhiteSpace(additionalSettings.Expressions.First().Value) == false)
                sb.Append(additionalSettings.Expressions.First().Value);

            return sb.ToString();
        }
    }
}