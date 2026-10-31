using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stimuli;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.MultitenantClrWorkflows;

public class Tests
{
    private readonly IServiceProvider _services;
    private readonly DefaultTenantAccessor _tenantAccessor;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseHttp()
                .AddWorkflow<HttpEndpointWorkflow>()
                .AddWorkflow<EventWorkflow>())
            .Build();
        _tenantAccessor = (DefaultTenantAccessor)_services.GetRequiredService<ITenantAccessor>();
    }

    [Fact(DisplayName = "A CLR workflow imported while activating the first tenant can be triggered from the second tenant")]
    public async Task SecondTenant_CanTriggerClrWorkflowsImportedByFirstTenant()
    {
        await ActivateTenantAsync("tenant-1");
        await ActivateTenantAsync("tenant-2");

        using var _ = PushTenant("tenant-2");
        using var scope = _services.CreateScope();
        var httpTriggers = await scope.ServiceProvider.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { Name = HttpStimulusNames.HttpEndpoint });
        Assert.Contains(httpTriggers, x => x.WorkflowDefinitionId == nameof(HttpEndpointWorkflow));

        var eventTriggerName = ActivityTypeNameHelper.GenerateTypeName<Event>();
        var result = await scope.ServiceProvider.GetRequiredService<IStimulusSender>().SendAsync(eventTriggerName, new EventStimulus(EventWorkflow.EventName));
        Assert.NotEmpty(result.WorkflowInstanceResponses);
    }

    private async Task ActivateTenantAsync(string tenantId)
    {
        using var _ = PushTenant(tenantId);
        using var scope = _services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
    }

    private IDisposable PushTenant(string tenantId) => _tenantAccessor.PushContext(new() { Id = tenantId, Name = tenantId });

    private class HttpEndpointWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new HttpEndpoint
            {
                Path = new("/multitenant-clr"),
                CanStartWorkflow = true
            };
        }
    }

    private class EventWorkflow : WorkflowBase
    {
        public const string EventName = "multitenant-clr-event";

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Event(EventName) { CanStartWorkflow = true };
        }
    }
}
