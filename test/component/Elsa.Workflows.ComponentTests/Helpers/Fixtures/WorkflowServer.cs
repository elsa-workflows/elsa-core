using Elsa.Caching;
using Elsa.Identity.Providers;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Decorators;
using Elsa.Workflows.ComponentTests.Materializers;
using Elsa.Workflows.ComponentTests.WorkflowProviders;
using Elsa.Workflows.Management;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static Elsa.Api.Client.RefitSettingsHelper;

namespace Elsa.Workflows.ComponentTests.Fixtures;

[UsedImplicitly]
public class WorkflowServer(Infrastructure infrastructure, string url) : WebApplicationFactory<Program>
{
    public TClient CreateApiClient<TClient>()
    {
        var client = CreateClient();
        client.BaseAddress = new(client.BaseAddress!, "/elsa/api");
        client.Timeout = TimeSpan.FromMinutes(1);
        return RestService.For<TClient>(client, CreateRefitSettings(Services));
    }

    public HttpClient CreateHttpWorkflowClient()
    {
        var client = CreateClient();
        client.BaseAddress = new(client.BaseAddress!, "/workflows/");
        client.Timeout = TimeSpan.FromMinutes(1);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseUrls(url);

        if (Program.ConfigureForTest == null)
        {
            Program.ConfigureForTest = elsa =>
            {
                elsa.AddWorkflowsFrom<WorkflowServer>();
                elsa.AddActivitiesFrom<WorkflowServer>();
                elsa.UseDefaultAuthentication(defaultAuthentication => defaultAuthentication.UseAdminApiKey());
                elsa.UseIdentity();
                elsa.UseWorkflowManagement();
                elsa.UseWorkflowRuntime();
                elsa.UseHttp();
            };
        }

        builder.ConfigureTestServices(services =>
        {
            services
                .AddSingleton<SignalManager>()
                .AddScoped<WorkflowEvents>()
                .AddScoped<WorkflowDefinitionEvents>()
                .AddSingleton<TriggerChangeTokenSignalEvents>()
                .AddScoped<IWorkflowMaterializer, TestWorkflowMaterializer>()
                .AddNotificationHandlersFrom<WorkflowServer>()
                .AddWorkflowDefinitionProvider<TestWorkflowProvider>()
                .Decorate<IChangeTokenSignaler, EventPublishingChangeTokenSignaler>()
                ;
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}