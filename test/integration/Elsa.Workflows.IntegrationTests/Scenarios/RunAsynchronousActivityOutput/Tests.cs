using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput;

public class Tests
{
    [Test]
    [DisplayName("Single activity outputs captured in activity execution record: $runAsynchronously")]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ActivityOutputCaptureTest(bool? runAsynchronously)
    {
        // Arrange
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var variable1 = new Variable<int>();
            workflowBuilder.Root = new Sequence
            {
                Variables =
                {
                    variable1
                },
                Activities =
                {
                    new SampleActivity
                    {
                        Id = "SampleActivity1",
                        RunAsynchronously = runAsynchronously,
                        Number1 = new(4),
                        Number2 = new(8),
                        Sum = new(variable1),
                        Product = null
                    }
                }
            };
        });

        var activityExecutionStore = new MemoryActivityExecutionStore(new());

        // Act
        var workflowFinishedRecord = await workflow.DispatchWorkflowAndRunToCompletion(
            configureElsa: elsa =>
            {
                elsa.UseWorkflowRuntime(workflowRuntime =>
                {
                    workflowRuntime.ActivityExecutionLogStore = sp => activityExecutionStore;
                });
            }
        );

        // Assert
        await Assert.That(workflowFinishedRecord).IsNotNull();
        await Assert.That(workflowFinishedRecord!.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(workflowFinishedRecord.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

        var activityExecutionRecord = await activityExecutionStore.FindAsync(new()
        {
            ActivityId = "SampleActivity1"
        });
        await Assert.That(activityExecutionRecord?.Outputs).IsNotNull();
        await Assert.That(activityExecutionRecord!.Outputs!.Count).IsEqualTo(2);
        await Assert.That(activityExecutionRecord.Outputs!.GetValue<int>("Sum")).IsEqualTo(12);
        await Assert.That(activityExecutionRecord.Outputs!.GetValue<int>("Product")).IsEqualTo(32);

        var activityOutputRegister = workflowFinishedRecord.WorkflowExecutionContext.GetActivityOutputRegister();
        await Assert.That(activityOutputRegister.FindOutputByActivityId("SampleActivity1", "Sum")).IsEqualTo(12);
        await Assert.That(activityOutputRegister.FindOutputByActivityId("SampleActivity1", "Product")).IsEqualTo(32);
    }

    [Test]
    [DisplayName("Parallel activity outputs captured in activity execution record: $runAsynchronously")]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ActivityOutputCaptureParallelTest(bool? runAsynchronously)
    {
        // Arrange
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            var variable1 = new Variable<int>();
            var variable2 = new Variable<int>();
            workflowBuilder.Root = new Elsa.Workflows.Activities.Parallel
            {
                Variables =
                {
                    variable1,
                    variable2,
                },
                Activities =
                {
                    new SampleActivity
                    {
                        Id = "SampleActivity1",
                        RunAsynchronously = runAsynchronously,
                        Number1 = new(4),
                        Number2 = new(8),
                        Sum = new(variable1),
                    },
                    new SampleActivity
                    {
                        Id = "SampleActivity2",
                        RunAsynchronously = runAsynchronously,
                        Number1 = new(2),
                        Number2 = new(7),
                        Product = new(variable2),
                    }
                }
            };
        });

        var activityExecutionStore = new MemoryActivityExecutionStore(new());

        // Act
        var workflowFinishedRecord = await workflow.DispatchWorkflowAndRunToCompletion(
            configureElsa: elsa =>
            {
                // Use the distributed runtime feature so its workflow runtime, bookmark worker, and dependencies are registered.
                elsa.UseWorkflowRuntime(workflowRuntime =>
                {
                    workflowRuntime.UseDistributedRuntime();
                    workflowRuntime.DistributedLockingOptions = options => options.AllowLocalLockProviderInDistributedRuntime = true;
                    workflowRuntime.ActivityExecutionLogStore = _ => activityExecutionStore;
                });
            });

        // Assert
        await Assert.That(workflowFinishedRecord).IsNotNull();
        await Assert.That(workflowFinishedRecord!.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(workflowFinishedRecord.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

        var activityExecutionRecord1 = await activityExecutionStore.FindAsync(new()
        {
            ActivityId = "SampleActivity1"
        });
        await Assert.That(activityExecutionRecord1?.Outputs).IsNotNull();
        await Assert.That(activityExecutionRecord1!.Outputs!.Count).IsEqualTo(2);
        await Assert.That(activityExecutionRecord1.Outputs!.GetValue<int>("Sum")).IsEqualTo(12);
        await Assert.That(activityExecutionRecord1.Outputs!.GetValue<int>("Product")).IsEqualTo(32);

        var activityExecutionRecord2 = await activityExecutionStore.FindAsync(new()
        {
            ActivityId = "SampleActivity2"
        });
        await Assert.That(activityExecutionRecord2?.Outputs).IsNotNull();
        await Assert.That(activityExecutionRecord2!.Outputs!.Count).IsEqualTo(2);
        await Assert.That(activityExecutionRecord2.Outputs!.GetValue<int>("Sum")).IsEqualTo(9);
        await Assert.That(activityExecutionRecord2.Outputs!.GetValue<int>("Product")).IsEqualTo(14);
    }
}
