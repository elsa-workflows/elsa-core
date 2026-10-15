using Elsa.Common.DistributedHosting;
using Elsa.Resilience;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Distributed;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DistributedWorkflowClientTests
{
    [Fact]
    public async Task CreateAndRunInstanceAsync_PreservesSchedulingMetadata()
    {
        var instanceManager = Substitute.For<IWorkflowInstanceManager>();
        var definitionService = Substitute.For<IWorkflowDefinitionService>();
        var runner = Substitute.For<IWorkflowRunner>();
        var activationGate = Substitute.For<IWorkflowActivationGate>();
        activationGate.EvaluateAsync(Arg.Any<Workflow>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new WorkflowActivationLease(true, null));

        var workflow = new Workflow { Id = "definition-1", Identity = new WorkflowIdentity("definition-1", 1, "version-1") };
        var node = new ActivityNode(workflow, "root");
        var graph = new WorkflowGraph(workflow, node, [node]);
        var definition = new WorkflowDefinition { Id = "version-1", DefinitionId = "definition-1" };
        definitionService.TryFindWorkflowGraphAsync(Arg.Any<WorkflowDefinitionHandle>(), Arg.Any<CancellationToken>())
            .Returns(new WorkflowGraphFindResult(definition, graph));

        var state = new WorkflowState
        {
            Id = "instance-1",
            DefinitionId = "definition-1",
            DefinitionVersionId = "version-1",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Pending
        };
        var instance = new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = "definition-1",
            DefinitionVersionId = "version-1",
            Status = WorkflowStatus.Running,
            WorkflowState = state
        };
        instanceManager.CreateWorkflowInstance(Arg.Any<Workflow>(), Arg.Any<WorkflowInstanceOptions>()).Returns(instance);

        RunWorkflowOptions? observedOptions = null;
        runner.RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedOptions = call.Arg<RunWorkflowOptions>();
                var resultState = new WorkflowState
                {
                    Id = "instance-1",
                    DefinitionId = "definition-1",
                    DefinitionVersionId = "version-1",
                    Status = WorkflowStatus.Running,
                    SubStatus = WorkflowSubStatus.Suspended
                };
                return Task.FromResult(new RunWorkflowResult(null!, resultState, workflow, null, Journal.Empty));
            });

        var lockProvider = Substitute.For<IDistributedLockProvider>();
        var distributedLock = Substitute.For<IDistributedLock>();
        var lockHandle = Substitute.For<IDistributedSynchronizationHandle>();
        lockProvider.CreateLock(Arg.Any<string>()).Returns(distributedLock);
        distributedLock.AcquireAsync(Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(lockHandle));

        var services = new ServiceCollection()
            .AddSingleton(instanceManager)
            .AddSingleton(definitionService)
            .AddSingleton(runner)
            .AddSingleton(Substitute.For<IWorkflowCanceler>())
            .AddSingleton(activationGate)
            .AddSingleton(Substitute.For<WorkflowStateMapper>())
            .AddSingleton(Substitute.For<ILogger<LocalWorkflowClient>>())
            .BuildServiceProvider();

        var client = new DistributedWorkflowClient(
            "instance-1",
            lockProvider,
            Substitute.For<ITransientExceptionDetector>(),
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            services,
            Substitute.For<ILogger<DistributedWorkflowClient>>());

        var response = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId("version-1"),
            SchedulingActivityExecutionId = "scheduled-execution-17",
            SchedulingWorkflowInstanceId = "parent-instance-8",
            SchedulingCallStackDepth = 6
        });

        Assert.False(response.CannotStart);
        Assert.NotNull(observedOptions);
        Assert.Equal("scheduled-execution-17", observedOptions.SchedulingActivityExecutionId);
        Assert.Equal("parent-instance-8", observedOptions.SchedulingWorkflowInstanceId);
        Assert.Equal(6, observedOptions.SchedulingCallStackDepth);
    }
}
