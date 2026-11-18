using System.Text.Json;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.IntegrationTests.Authorization;

public class HttpEndpointCredentialHistoryTests
{
    private const string Credential = "synthetic-credential-marker";
    private const string RequestLabel = "ordinary-header-value";

    [Fact]
    public async Task OnlyNonCredentialHeadersReachExecutionHistory()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddElsa(elsa => elsa
            .UseWorkflowManagement()
            .UseWorkflowRuntime()
            .UseWorkflows()
            .UseHttp()
            .AddWorkflow<CredentialHistoryWorkflow>());

        await using var app = builder.Build();
        app.UseWorkflows();
        await app.StartAsync();
        await app.Services.PopulateRegistriesAsync();

        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/workflows/header-history");
        request.Headers.TryAddWithoutValidation("aUtHoRiZaTiOn", Credential);
        request.Headers.TryAddWithoutValidation("CoOkIe", Credential);
        request.Headers.TryAddWithoutValidation("X-Api-Key", Credential);
        request.Headers.TryAddWithoutValidation("X-Request-Label", RequestLabel);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.Equal("live", await response.Content.ReadAsStringAsync());

        await using var scope = app.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
        var records = await store.FindManyAsync(new());
        var endpoint = Assert.Single(records, record => record.Outputs?.ContainsKey("Headers") == true);
        var output = JsonSerializer.Serialize(endpoint.Outputs);
        Assert.Contains(RequestLabel, output);
        Assert.DoesNotContain(Credential, output);
        Assert.DoesNotContain(Credential, endpoint.SerializedSnapshot?.SerializedOutputs ?? "");
    }

    public class CredentialHistoryWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new Sequence
            {
                Activities =
                [
                    new HttpEndpoint { Path = new("/header-history"), CanStartWorkflow = true },
                    new WriteHttpResponse
                    {
                        Content = new(context => context.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.Headers.Authorization == Credential
                            ? "live"
                            : "missing")
                    }
                ]
            };
        }
    }
}
