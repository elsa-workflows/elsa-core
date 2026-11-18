using System.Net;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Middleware.Notification.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredTriggerCleanupInvalidatesAmbiguityCachedBeforeRemoval(bool cacheTriggerStore)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddElsa(elsa => elsa
            .UseWorkflowManagement()
            .UseWorkflowRuntime(runtime =>
            {
                if (cacheTriggerStore)
                {
                    runtime.UseCache();
                }
            })
            .UseWorkflows()
            .UseHttp(http => http.UseCache())
            .AddWorkflow<FirstTriggerWorkflow>()
            .AddWorkflow<SecondTriggerWorkflow>());

        await using var app = builder.Build();
        app.UseWorkflows();
        await app.StartAsync();
        await app.Services.PopulateRegistriesAsync();
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var triggerStore = services.GetRequiredService<ITriggerStore>();
        var first = Assert.Single(await triggerStore.FindManyAsync(new() { WorkflowDefinitionId = "first-trigger" }));
        var lookup = services.GetRequiredService<IHttpWorkflowLookupService>();

        // Read after the pre-removal subscribers have completed and before the store write.
        var lookupRefilled = false;
        services.GetRequiredService<INotificationPipeline>().Setup(pipeline => pipeline
            .Use(next => async context =>
            {
                await next(context);
                if (context.Notification is WorkflowTriggersIndexed indexed && indexed.IndexedWorkflowTriggers.RemovedTriggers.Any(trigger => trigger.Id == first.Id))
                {
                    var duringRemoval = await lookup.FindWorkflowAsync(first.Hash!, context.CancellationToken);
                    Assert.Equal(2, duringRemoval?.Triggers.Count);
                    Assert.Null(duringRemoval?.WorkflowGraph);
                    lookupRefilled = true;
                }
            })
            .UseNotificationHandlers());

        var notification = new WorkflowDefinitionVersionsDeleting([first.WorkflowDefinitionVersionId])
        {
            RequireCompleteTriggerCleanup = true
        };
        await services.GetRequiredService<INotificationSender>().SendAsync(notification);

        Assert.True(lookupRefilled);
        var afterRemoval = await lookup.FindWorkflowAsync(first.Hash!);
        Assert.NotNull(afterRemoval?.WorkflowGraph);
        Assert.Equal("second-trigger", Assert.Single(afterRemoval.Triggers).WorkflowDefinitionId);
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
