using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class LocalWorkflowClientTests
{
    private readonly IWorkflowInstanceManager _workflowInstanceManager = Substitute.For<IWorkflowInstanceManager>();
    private readonly IWorkflowDefinitionService _workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
    private readonly IWorkflowRunner _workflowRunner = Substitute.For<IWorkflowRunner>();
    private readonly IWorkflowCanceler _workflowCanceler = Substitute.For<IWorkflowCanceler>();
    private readonly WorkflowStateMapper _workflowStateMapper = Substitute.For<WorkflowStateMapper>();
    private readonly ILogger<LocalWorkflowClient> _logger = Substitute.For<ILogger<LocalWorkflowClient>>();

    [Fact]
    public async Task CreateInstanceAsync_ThrowsWorkflowDefinitionNotFoundException_WhenDefinitionDoesNotExist()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("non-existent-definition");
        var request = new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var findResult = new WorkflowGraphFindResult(null, null);

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowDefinitionNotFoundException>(() =>
            client.CreateInstanceAsync(request));
    }

    [Fact]
    public async Task CreateInstanceAsync_ThrowsWorkflowMaterializerNotFoundException_WhenMaterializerNotAvailable()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition");
        var request = new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var definition = new WorkflowDefinition
        {
            Id = "def-1",
            DefinitionId = "test-definition",
            MaterializerName = "unavailable-materializer"
        };

        var findResult = new WorkflowGraphFindResult(definition, null); // Null graph indicates materializer not available

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowMaterializerNotFoundException>(() => client.CreateInstanceAsync(request));
    }

    [Fact]
    public async Task CreateAndRunInstanceAsync_ThrowsWorkflowDefinitionNotFoundException_WhenDefinitionDoesNotExist()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("non-existent-definition");
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var findResult = new WorkflowGraphFindResult(null, null);

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowDefinitionNotFoundException>(() =>
            client.CreateAndRunInstanceAsync(request));
    }

    [Fact]
    public async Task CreateAndRunInstanceAsync_ThrowsWorkflowMaterializerNotFoundException_WhenMaterializerNotAvailable()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition");
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var definition = new WorkflowDefinition
        {
            Id = "def-1",
            DefinitionId = "test-definition",
            MaterializerName = "unavailable-materializer"
        };

        var findResult = new WorkflowGraphFindResult(definition, null);

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowMaterializerNotFoundException>(() =>
            client.CreateAndRunInstanceAsync(request));
    }

    private LocalWorkflowClient CreateClient()
    {
        return new(
            "test-workflow-instance-id",
            _workflowInstanceManager,
            _workflowDefinitionService,
            _workflowRunner,
            _workflowCanceler,
            _workflowStateMapper,
            _logger);
    }
}
