using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.UnitTests.Extensions;

public class WorkflowDispatchCommandFactoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCommand_UsesFallbackInstanceId_WhenRequestDoesNotSpecifyInstanceId(string? requestInstanceId)
    {
        var request = new DispatchWorkflowDefinitionRequest("definition-version-1")
        {
            InstanceId = requestInstanceId
        };

        var command = WorkflowDispatchCommandFactory.CreateCommand(request, "generated-instance");

        Assert.Equal("generated-instance", command.InstanceId);
        Assert.True(command.SkipIfInstanceExists);
    }

    [Fact]
    public void CreateCommand_PreservesRequestInstanceId_WhenSpecified()
    {
        var request = new DispatchWorkflowDefinitionRequest("definition-version-1")
        {
            InstanceId = "requested-instance"
        };

        var command = WorkflowDispatchCommandFactory.CreateCommand(request, "generated-instance");

        Assert.Equal("requested-instance", command.InstanceId);
        Assert.False(command.SkipIfInstanceExists);
    }
}
