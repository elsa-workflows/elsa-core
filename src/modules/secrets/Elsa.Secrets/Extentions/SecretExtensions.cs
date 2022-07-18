using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elsa.Secrets.Models;
using Newtonsoft.Json;

namespace Elsa.Secrets.Extentions
{
    public static class SecretExtensions
    {
        public static string GetValue(this Secret secret)
        {
            switch (secret.Type)
            {
                case SecretType.MsSql:
                    {
                        return ConvertToCredentialString(secret.PropertiesJson, "=", ";", includePropertyNamesInValues: true);
                    }
                case SecretType.PostgreSql:
                    {
                        return ConvertToCredentialString(secret.PropertiesJson, "=", ";", includePropertyNamesInValues: true);
                    }
                case SecretType.AuthorizationHeader:
                    {
                        return ConvertToCredentialString(secret.PropertiesJson, ":", string.Empty, includePropertyNamesInValues: false);
                    }
                case SecretType.Ampq:
                default: return string.Empty;
            }
        }

        private static string ConvertToCredentialString(string propertiesJson, string nameValueSeparator, string propertySeparator, bool includePropertyNamesInValues)
        {
            var properties = JsonConvert.DeserializeObject(propertiesJson).ConvertTo<ICollection<SecretProperty>>();
            var sb = new StringBuilder();

            foreach (var property in properties.Where(x => x.Expressions.Count > 0))
            {
                var propertyNameWithSeparator = includePropertyNamesInValues ? (property.Name + nameValueSeparator) : string.Empty;

                sb.Append($"{propertyNameWithSeparator}{property.Expressions.First().Value}{propertySeparator}");
            }

            return sb.ToString();
        }
    }
}
