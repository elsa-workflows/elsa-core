using Elsa.Expressions.Helpers;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ProjectedEnumerableToArray;

public class EnumerableProjectionTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "JavaScript should convert a Select-projected IEnumerable variable to an array")]
    public async Task Should_Convert_Select_Projected_Enumerable_To_Array()
    {
        // Arrange
        var workflow = new EnumerableProjectionTestWorkflow();

        // Act
        var result = await _fixture.RunWorkflowAsync(workflow);

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus); // If conversion failed, workflow will have faulted.
        var variableManager = _fixture.Services.GetRequiredService<IWorkflowInstanceVariableManager>();
        var messagesVariable = (await variableManager.GetVariablesAsync(result.WorkflowExecutionContext)).FirstOrDefault(x => x.Variable.Name == "Messages");
        var messages = messagesVariable?.Value.ConvertTo<string[]>();
        
        Assert.NotNull(messages);
        Assert.Equal(5, messages.Length);

        Assert.All(messages, message =>
        {
            Assert.Contains("Name:", message);
            Assert.Contains("ID:", message);
        });
    }
}