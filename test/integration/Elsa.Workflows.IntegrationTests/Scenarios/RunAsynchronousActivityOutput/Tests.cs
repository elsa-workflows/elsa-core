using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput;

public class Tests
{
    [Theory(DisplayName = "Activity outputs captured in activity execution record")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ActivityOutputCaptureTest(bool runAsynchronously)
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
        Assert.NotNull(workflowFinishedRecord);
        Assert.Equal(WorkflowStatus.Finished, workflowFinishedRecord.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowFinishedRecord.WorkflowState.SubStatus);

        var activityExecutionRecord = await activityExecutionStore.FindAsync(new()
        {
            ActivityId = "SampleActivity1"
        });
        Assert.NotNull(activityExecutionRecord?.Outputs);
        Assert.Equal(2, activityExecutionRecord.Outputs!.Count);
        Assert.Equal(12, activityExecutionRecord.Outputs!.GetValue<int>("Sum"));
        Assert.Equal(32, activityExecutionRecord.Outputs!.GetValue<int>("Product"));

        var activityOutputRegister = workflowFinishedRecord.WorkflowExecutionContext.GetActivityOutputRegister();
        Assert.Equal(12, activityOutputRegister.FindOutputByActivityId("SampleActivity1", "Sum"));
        Assert.Equal(32, activityOutputRegister.FindOutputByActivityId("SampleActivity1", "Product"));
    }

    [Theory(DisplayName = "Activity outputs captured in activity execution record")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ActivityOutputCaptureParallelTest(bool runAsynchronously)
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
                elsa.UseWorkflowRuntime(workflowRuntime =>
                {
                    workflowRuntime.ActivityExecutionLogStore = sp => activityExecutionStore;
                });
            });

        // Assert
        Assert.NotNull(workflowFinishedRecord);
        Assert.Equal(WorkflowStatus.Finished, workflowFinishedRecord.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowFinishedRecord.WorkflowState.SubStatus);

        var activityExecutionRecord1 = await activityExecutionStore.FindAsync(new()
        {
            ActivityId = "SampleActivity1"
        });
        Assert.NotNull(activityExecutionRecord1?.Outputs);
        Assert.Equal(2, activityExecutionRecord1.Outputs!.Count);
        Assert.Equal(12, activityExecutionRecord1.Outputs!.GetValue<int>("Sum"));
        Assert.Equal(32, activityExecutionRecord1.Outputs!.GetValue<int>("Product"));

        var activityExecutionRecord2 = await activityExecutionStore.FindAsync(new()
        {
            ActivityId = "SampleActivity2"
        });
        Assert.NotNull(activityExecutionRecord2?.Outputs);
        Assert.Equal(2, activityExecutionRecord2.Outputs!.Count);
        Assert.Equal(9, activityExecutionRecord2.Outputs!.GetValue<int>("Sum"));
        Assert.Equal(14, activityExecutionRecord2.Outputs!.GetValue<int>("Product"));
    }
}