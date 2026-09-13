using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.UnitTests.Extensions;

public class WorkflowDispatchCommandFactoryTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task CreateCommand_UsesFallbackInstanceId_WhenRequestDoesNotSpecifyInstanceId(string? requestInstanceId)
    {
        var request = new DispatchWorkflowDefinitionRequest("definition-version-1")
        {
            InstanceId = requestInstanceId
        };

        var command = WorkflowDispatchCommandFactory.CreateCommand(request, "generated-instance");

        await Assert.That(command.InstanceId).IsEqualTo("generated-instance");
        await Assert.That(command.SkipIfInstanceExists).IsTrue();
    }

    [Test]
    public async Task CreateCommand_PreservesRequestInstanceId_WhenSpecified()
    {
        var request = new DispatchWorkflowDefinitionRequest("definition-version-1")
        {
            InstanceId = "requested-instance"
        };

        var command = WorkflowDispatchCommandFactory.CreateCommand(request, "generated-instance");

        await Assert.That(command.InstanceId).IsEqualTo("requested-instance");
        await Assert.That(command.SkipIfInstanceExists).IsFalse();
    }

    [Test]
    public async Task CreateCommand_DoesNotSkipExistingInstance_WhenFallbackInstanceIdIsMissing()
    {
        var request = new DispatchWorkflowDefinitionRequest("definition-version-1");

        var command = WorkflowDispatchCommandFactory.CreateCommand(request);

        await Assert.That(command.InstanceId).IsNull();
        await Assert.That(command.SkipIfInstanceExists).IsFalse();
    }
}
