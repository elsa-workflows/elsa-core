using System.Net;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.IntegrationTests.Authorization;

public class HttpEndpointAmbiguityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnonymousAmbiguousResponseContainsCorrelationAndNoResourceIdentifiers(bool resume)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddElsa(elsa => elsa
            .UseWorkflowManagement()
            .UseWorkflowRuntime()
            .UseWorkflows()
            .UseHttp()
            .AddWorkflow<FirstTriggerWorkflow>()
            .AddWorkflow<SecondTriggerWorkflow>()
            .AddWorkflow<FirstResumeWorkflow>()
            .AddWorkflow<SecondResumeWorkflow>());

        await using var app = builder.Build();
        app.UseWorkflows();
        await app.StartAsync();
        await app.Services.PopulateRegistriesAsync();
        using var client = app.GetTestClient();

        if (resume)
        {
            using var first = await client.GetAsync("/workflows/ambiguity-start-a");
            using var second = await client.GetAsync("/workflows/ambiguity-start-b");
            first.EnsureSuccessStatusCode();
            second.EnsureSuccessStatusCode();
        }

        using var response = await client.GetAsync(resume ? "/workflows/ambiguity-resume" : "/workflows/ambiguity-trigger");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["correlationId", "errorMessage"], payload.RootElement.EnumerateObject().Select(property => property.Name).Order());
        Assert.False(string.IsNullOrWhiteSpace(payload.RootElement.GetProperty("correlationId").GetString()));
    }

    public class FirstTriggerWorkflow() : TriggerWorkflow("first-trigger");
    public class SecondTriggerWorkflow() : TriggerWorkflow("second-trigger");
    public class FirstResumeWorkflow() : ResumeWorkflow("first-resume", "/ambiguity-start-a");
    public class SecondResumeWorkflow() : ResumeWorkflow("second-resume", "/ambiguity-start-b");

    public abstract class TriggerWorkflow(string definitionId) : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(definitionId);
            builder.Root = new HttpEndpoint { Path = new("/ambiguity-trigger"), CanStartWorkflow = true };
        }
    }

    public abstract class ResumeWorkflow(string definitionId, string startPath) : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId(definitionId);
            builder.Root = new Sequence
            {
                Activities =
                [
                    new HttpEndpoint { Path = new(startPath), CanStartWorkflow = true },
                    new HttpEndpoint { Path = new("/ambiguity-resume") }
                ]
            };
        }
    }
}
