using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing;

/// <summary>
/// Verifies that a <see cref="Cron"/> trigger with a blank expression is treated as "no trigger"
/// (so the workflow can still be published / indexed), while valid expressions still produce a trigger.
/// </summary>
public class EmptyCronTriggerTests : IAsyncLifetime
{
    private readonly IServiceProvider _services;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IWorkflowGraphBuilder _workflowGraphBuilder;

    public EmptyCronTriggerTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
        _triggerIndexer = _services.GetRequiredService<ITriggerIndexer>();
        _workflowGraphBuilder = _services.GetRequiredService<IWorkflowGraphBuilder>();
    }

    public Task InitializeAsync() => _services.PopulateRegistriesAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory(DisplayName = "A Cron trigger with a blank expression indexes no triggers")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankCronExpression_IndexesNoTriggers(string cronExpression)
    {
        var workflow = await BuildCronWorkflowAsync(cronExpression);

        var triggers = await _triggerIndexer.GetTriggersAsync(workflow);

        Assert.Empty(triggers);
    }

    [Fact(DisplayName = "A Cron trigger with a valid expression indexes a single trigger")]
    public async Task ValidCronExpression_IndexesSingleTrigger()
    {
        var workflow = await BuildCronWorkflowAsync("0 0 0 * * *");

        var triggers = (await _triggerIndexer.GetTriggersAsync(workflow)).ToList();

        var trigger = Assert.Single(triggers);
        var payload = Assert.IsType<CronTriggerPayload>(trigger.Payload);
        Assert.Equal("0 0 0 * * *", payload.CronExpression);
    }

    private async Task<Workflow> BuildCronWorkflowAsync(string cronExpression)
    {
        var workflow = new Workflow
        {
            Identity = new("CronWorkflow", 1, "1", Tenant.DefaultTenantId),
            Root = new Cron
            {
                CronExpression = new Input<string>(cronExpression),
                CanStartWorkflow = true
            }
        };

        var workflowGraph = await _workflowGraphBuilder.BuildAsync(workflow);
        return workflowGraph.Workflow;
    }
}
