using Elsa.Expressions.Helpers;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ProjectedEnumerableToArray;

public class EnumerableProjectionTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("JavaScript should convert a Select-projected IEnumerable variable to an array")]
    public async Task Should_Convert_Select_Projected_Enumerable_To_Array()
    {
        // Arrange
        var workflow = new EnumerableProjectionTestWorkflow();

        // Act
        var result = await _fixture.RunWorkflowAsync(workflow);

        // Assert
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished); // If conversion failed, workflow will have faulted.
        var variableManager = _fixture.Services.GetRequiredService<IWorkflowInstanceVariableManager>();
        var messagesVariable = (await variableManager.GetVariablesAsync(result.WorkflowExecutionContext)).FirstOrDefault(x => x.Variable.Name == "Messages");
        var messages = messagesVariable?.Value.ConvertTo<string[]>();
        
        await Assert.That(messages).IsNotNull();
        var projectedMessages = messages!;
        await Assert.That(projectedMessages.Length).IsEqualTo(5);

        foreach (var message in projectedMessages)
        {
            await Assert.That(message).Contains("Name:", StringComparison.CurrentCulture);
            await Assert.That(message).Contains("ID:", StringComparison.CurrentCulture);
        }
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
