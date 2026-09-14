using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Extensions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Alterations.IntegrationTests;

public sealed class RetrySequenceTests : IAsyncLifetime
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _output = new();
    private readonly RetryProbe _probe = new();

    public RetrySequenceTests(ITestOutputHelper output)
    {
        _services = new TestApplicationBuilder(output)
            .WithCapturingTextWriter(_output)
            .ConfigureServices(services => services.AddSingleton(_probe))
            .ConfigureElsa(elsa => elsa.UseAlterations())
            .AddWorkflow<RetrySequenceWorkflow>()
            .AddWorkflow<RetryStandaloneSequenceWorkflow>()
            .AddActivitiesFrom<RetryBookmarkActivity>()
            .Build();
    }

    public Task InitializeAsync() => _services.PopulateRegistriesAsync();
    public async Task DisposeAsync() => await ((IAsyncDisposable)_services).DisposeAsync();

    [Theory]
    [InlineData(nameof(RetrySequenceWorkflow))]
    [InlineData(nameof(RetryStandaloneSequenceWorkflow))]
    public async Task RetriedChildCompletesItsSequenceAndPreservesTheNextBookmark(string definitionId)
    {
        var runtime = _services.GetRequiredService<IWorkflowRuntime>();
        var client = await runtime.CreateClientAsync();
        await client.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published)
        });
        var firstRun = await client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        var faultedState = await client.ExportStateAsync();
        var incident = Assert.Single(faultedState.Incidents);
        Assert.Equal("Retry", incident.ActivityId);
        var faultedContext = Assert.Single(faultedState.ActivityExecutionContexts, x => x.Status == ActivityStatus.Faulted);
        var sequenceContextId = faultedContext.ParentContextId;
        Assert.Equal(1, _probe.Attempts);
        Assert.Empty(_output.Lines);

        var alterations = new IAlteration[] { new ScheduleActivity { ActivityInstanceId = faultedContext.Id } };
        var results = await _services.GetRequiredService<IAlterationRunner>().RunAsync([firstRun.WorkflowInstanceId], alterations);
        Assert.True(Assert.Single(results).IsSuccessful);

        var alteredState = await client.ExportStateAsync();
        var callbacks = alteredState.CompletionCallbacks
            .Where(x => x.ChildNodeId == faultedContext.ScheduledActivityNodeId)
            .ToList();
        Assert.NotEmpty(callbacks);
        Assert.All(callbacks, callback => Assert.Equal(sequenceContextId, callback.OwnerInstanceId));

        await client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        var retriedState = await client.ExportStateAsync();
        var retryBookmark = Assert.Single(retriedState.Bookmarks);
        Assert.Equal(faultedContext.Id, retryBookmark.ActivityInstanceId);
        Assert.Equal(2, _probe.Attempts);

        var afterRetry = await client.RunInstanceAsync(new() { BookmarkId = retryBookmark.Id });
        var nextState = await client.ExportStateAsync();
        var nextBookmark = Assert.Single(nextState.Bookmarks);
        Assert.Equal(WorkflowStatus.Running, afterRetry.Status);
        Assert.Equal(["Sequence finished"], _output.Lines);
        Assert.Equal(faultedState.Incidents.Count, nextState.Incidents.Count);
        Assert.NotEqual(retryBookmark.ActivityInstanceId, nextBookmark.ActivityInstanceId);

        var finalRun = await client.RunInstanceAsync(new() { BookmarkId = nextBookmark.Id });
        Assert.Equal(WorkflowStatus.Finished, finalRun.Status);
        Assert.Equal(["Sequence finished", "Done"], _output.Lines);
        Assert.Equal(2, _probe.Attempts);
        Assert.Empty((await client.ExportStateAsync()).Bookmarks);
    }

    public sealed class RetryProbe
    {
        public int Attempts { get; set; }
    }

    public sealed class RetryBookmarkActivity : Activity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
            if (++context.GetRequiredService<RetryProbe>().Attempts == 1)
            {
                throw new InvalidOperationException("Transient test failure");
            }

            context.CreateBookmark(new CreateBookmarkArgs());
        }
    }

    public class RetrySequenceWorkflow : WorkflowBase
    {
        protected virtual bool UseFlowchart => true;

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WorkflowOptions.IncidentStrategyType = typeof(ContinueWithIncidentsStrategy);
            var sequence = new Sequence
            {
                Activities = { new RetryBookmarkActivity { Id = "Retry" }, new WriteLine("Sequence finished") }
            };
            var next = new Event("Next");
            var end = new WriteLine("Done");
            builder.Root = UseFlowchart
                ? new Flowchart
                {
                    Activities = { sequence, next, end },
                    Connections = { new Connection(sequence, next), new Connection(next, end) }
                }
                : new Sequence { Activities = { sequence, next, end } };
        }
    }

    public sealed class RetryStandaloneSequenceWorkflow : RetrySequenceWorkflow
    {
        protected override bool UseFlowchart => false;
    }
}
