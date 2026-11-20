using Elsa.Expressions.Helpers;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput;

[Collection(nameof(NonSerializableOutputTestsCollection))]
public class NonSerializableOutputTests
{
    [Fact(DisplayName = "Non-serializable dictionary output doesn't fault a background activity in strict conversion mode")]
    public async Task NonSerializableDictionaryOutputTest()
    {
        // Arrange
        var workflow = new TestWorkflow(workflowBuilder =>
        {
            workflowBuilder.Root = new NonSerializableDynamicOutputsActivity
            {
                RunAsynchronously = true,
                Outputs = new()
                {
                    ["name"] = new()
                }
            };
        });

        var originalStrictMode = ObjectConverter.StrictMode;

        try
        {
            ObjectConverter.StrictMode = true;

            // Act
            var workflowFinishedRecord = await workflow.DispatchWorkflowAndRunToCompletion();

            // Assert
            Assert.NotNull(workflowFinishedRecord);
            Assert.Equal(WorkflowStatus.Finished, workflowFinishedRecord.WorkflowState.Status);
            Assert.Equal(WorkflowSubStatus.Finished, workflowFinishedRecord.WorkflowState.SubStatus);
        }
        finally
        {
            ObjectConverter.StrictMode = originalStrictMode;
        }
    }
}

/// <summary>
/// <see cref="ObjectConverter.StrictMode"/> is global, so these tests must not run in parallel with other tests.
/// </summary>
[CollectionDefinition(nameof(NonSerializableOutputTestsCollection), DisableParallelization = true)]
public class NonSerializableOutputTestsCollection;
