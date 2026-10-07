using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Extensions;
using Elsa.Alterations.Services;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Elsa.Alterations.IntegrationTests;

public class ExecutionCycleOwnershipTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AlterationRetainsHandleThroughPostCommitImport(bool failImport)
    {
        await using var services = (ServiceProvider)new TestApplicationBuilder(output)
            .ConfigureElsa(elsa => elsa.UseAlterations())
            .Build();
        await services.PopulateRegistriesAsync();
        var graph = await services.GetRequiredService<IWorkflowGraphBuilder>().BuildAsync(Workflow.FromActivity(new WriteLine("test")));
        var context = await WorkflowExecutionContext.CreateAsync(services, graph, "alteration-instance");
        var extractor = services.GetRequiredService<IWorkflowStateExtractor>();
        var state = extractor.Extract(context);
        var registry = services.GetRequiredService<IExecutionCycleRegistry>();
        var client = Substitute.For<IWorkflowClient>();
        client.ExportStateAsync(Arg.Any<CancellationToken>()).Returns(state);
        var runtime = Substitute.For<IWorkflowRuntime>();
        runtime.CreateClientAsync(state.Id, Arg.Any<CancellationToken>()).Returns(new ValueTask<IWorkflowClient>(client));
        var definitions = Substitute.For<IWorkflowDefinitionService>();
        definitions.FindWorkflowGraphAsync(state.DefinitionVersionId, Arg.Any<CancellationToken>()).Returns(graph);
        var started = new TaskCompletionSource<ExecutionCycleHandle>(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueImport = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = failImport ? new InvalidOperationException("import failed") : null;
        client.ImportStateAsync(Arg.Any<WorkflowState>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            started.SetResult(Assert.Single(registry.ListActiveCycles()));
            await continueImport.Task;
            if (failure != null)
                throw failure;
        });
        var commit = new CapturingCommit(services.GetRequiredService<ICommitStateHandler>(), registry);
        var runner = new DefaultAlterationRunner(runtime, services.GetRequiredService<IWorkflowExecutionPipeline>(),
            definitions, extractor, commit, services.GetRequiredService<ISystemClock>(), services);

        var run = runner.RunAsync(state.Id, Array.Empty<IAlteration>());
        ExecutionCycleHandle? handle = null;
        try
        {
            handle = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Same(commit.Handle, handle);
            Assert.False(handle.Disposed.IsCompleted);
            Assert.False(run.IsCompleted);
        }
        finally
        {
            continueImport.TrySetResult();
        }
        var actual = await Record.ExceptionAsync(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(failure, actual);
        Assert.Equal(0, registry.ActiveCount);
        Assert.True(handle!.Disposed.IsCompletedSuccessfully);
    }

    private sealed class CapturingCommit(ICommitStateHandler inner, IExecutionCycleRegistry registry) : ICommitStateHandler
    {
        public ExecutionCycleHandle? Handle { get; private set; }
        public Task CommitAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default) => inner.CommitAsync(context, cancellationToken);
        public async Task CommitAsync(WorkflowExecutionContext context, WorkflowState state, CancellationToken cancellationToken = default)
        {
            Handle = Assert.Single(registry.ListActiveCycles());
            await inner.CommitAsync(context, state, cancellationToken);
            Assert.Same(Handle, Assert.Single(registry.ListActiveCycles()));
        }
    }
}
