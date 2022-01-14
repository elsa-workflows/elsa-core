using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks.ActivityTypes
{
    public class MultitenantWebhookActivityTypeProvider : WebhookActivityTypeProvider
    {
        private readonly ITenantProvider _tenantProvider;

        public MultitenantWebhookActivityTypeProvider(IActivityActivator activityActivator, IServiceScopeFactory serviceScopeFactory, ITenantProvider tenantProvider): base(activityActivator, serviceScopeFactory) => _tenantProvider = tenantProvider;

        public override async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var tenant = _tenantProvider.GetCurrentTenant();

            using var scope = _serviceScopeFactory.CreateScopeForTenant(tenant);

            var webhookDefinitionStore = scope.ServiceProvider.GetRequiredService<IWebhookDefinitionStore>();
            var specification = Specification<WebhookDefinition>.Identity;
            var definitions = await webhookDefinitionStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            var activityTypes = new List<ActivityType>();

            foreach (var definition in definitions)
            {
                var activity = CreateWebhookActivityType(definition);
                activityTypes.Add(activity);
            }

            return activityTypes;
        }
    }
}
