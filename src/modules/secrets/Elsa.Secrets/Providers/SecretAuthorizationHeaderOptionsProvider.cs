using System.Linq;
using System.Reflection;
using Elsa.Activities.Http;
using Elsa.Design;
using Elsa.Metadata;
using Elsa.Secrets.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Providers
{
    public class SecretAuthorizationHeaderOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SecretAuthorizationHeaderOptionsProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public object? GetOptions(PropertyInfo property)
        {
            if (property.Name != nameof(SendHttpRequest.Authorization)) return null;

            using var scope = _scopeFactory.CreateScope();
            var secretsProvider = scope.ServiceProvider.GetRequiredService<ISecretsProvider>();

            var secretsAuth = secretsProvider.GetSecretsForSelectListAsync(Constants.SecretType_AuthorizationHeader).Result;

            var items = secretsAuth.Select(x => new SelectListItem(x.Item1, x.Item2)).ToList();
            items.Insert(0, new SelectListItem("", "empty"));

            return new SelectList { Items = items };
        }
    }
}