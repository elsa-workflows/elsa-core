using System.Linq;
using System.Reflection;
using Elsa.Design;
using Elsa.Metadata;
using Elsa.Secrets.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Providers
{
    /// <summary>
    /// Options provider to list only secret names. Value formated as [Secret Type]:[Secret Name]
    /// </summary>
    /// <remarks>
    /// Input value is then used within an activity to call <see cref="ISecretsProvider.GetSecretByName(string, string)">ISecretsProvider.GetSecrets(type,name)</see>,
    /// so that the secret stays stored in secret store and not workflow definition.
    /// </remarks>
    public class SecretNamesOptionsProvider : IActivityPropertyOptionsProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SecretNamesOptionsProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public object? GetOptions(PropertyInfo property)
        {
            using var scope = _scopeFactory.CreateScope();
            var secretsManager = scope.ServiceProvider.GetRequiredService<ISecretsManager>();
            var secrets = secretsManager.GetSecrets().Result;

            var items = secrets.Where(x => string.IsNullOrWhiteSpace(x.Name) == false)
                .Select(x => new SelectListItem($"{(string.IsNullOrWhiteSpace(x.DisplayName) == false ? x.DisplayName : x.Name)} ({x.Type})", $"{x.Type}:{x.Name!}")).ToList();
            items.Insert(0, new SelectListItem("", "empty"));

            return new SelectList { Items = items };
        }
    }
}
