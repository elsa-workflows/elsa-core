using System.Linq;
using System.Reflection;
using Elsa.Design;
using Elsa.Metadata;
using Elsa.Secrets.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Providers
{
    /// <summary>
    /// Options provider to list HTTP authorization header secrets.
    /// </summary>
    public class SecretAuthorizationHeaderOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SecretAuthorizationHeaderOptionsProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public object? GetOptions(PropertyInfo property)
        {
            using var scope = _scopeFactory.CreateScope();
            var secretsProvider = scope.ServiceProvider.GetRequiredService<ISecretsProvider>();

            var secretsAuth = secretsProvider.GetSecretsDictionaryAsync(Constants.SecretType_AuthorizationHeader).Result;
            var secretsOAuth = secretsProvider.GetSecretsDictionaryAsync(Constants.SecretType_OAuth2).Result;

            var items = secretsAuth.Concat(secretsOAuth).Select(x => new SelectListItem(x.Value, x.Key)).ToList();
            items.Insert(0, new SelectListItem("", "empty"));

            return new SelectList { Items = items };
        }
    }
}