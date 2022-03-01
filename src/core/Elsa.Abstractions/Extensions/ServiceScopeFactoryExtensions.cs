using Elsa.Abstractions.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class ServiceScopeFactoryExtensions
    {
        public static IServiceScope CreateScopeForTenant(this IServiceScopeFactory scopeFactory, ITenant tenant)
        {
            var scope = scopeFactory.CreateScope();
            scope.SetCurrentTenant(tenant);

            return scope;
        }
    }
}
