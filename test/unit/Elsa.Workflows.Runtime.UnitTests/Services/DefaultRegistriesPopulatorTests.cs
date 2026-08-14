using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Notifications;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DefaultRegistriesPopulatorTests
{
    private readonly IWorkflowDefinitionStorePopulator _workflowDefinitionStorePopulatorMock;
    private readonly IActivityRegistryPopulator _activityRegistryPopulatorMock;
    private readonly INotificationSender _notificationSenderMock;
    private readonly DefaultRegistriesPopulator _populator;

    public DefaultRegistriesPopulatorTests()
    {
        _workflowDefinitionStorePopulatorMock = Substitute.For<IWorkflowDefinitionStorePopulator>();
        _activityRegistryPopulatorMock = Substitute.For<IActivityRegistryPopulator>();
        _notificationSenderMock = Substitute.For<INotificationSender>();

        _populator = new(
            _workflowDefinitionStorePopulatorMock,
            _activityRegistryPopulatorMock,
            _notificationSenderMock);
    }

    [Fact(DisplayName = "PopulateAsync publishes WorkflowDefinitionsReloaded notification")]
    public async Task PopulateAsync_PublishesWorkflowDefinitionsReloadedNotification()
    {
        // Arrange
        var workflowDefinitions = new List<WorkflowDefinition>
        {
            new()
            {
                Id = "1",
                DefinitionId = "def-1",
                Version = 1
            },
            new()
            {
                Id = "2",
                DefinitionId = "def-2",
                Version = 1
            }
        };

        _workflowDefinitionStorePopulatorMock
            .PopulateStoreAsync(true, Arg.Any<CancellationToken>())
            .Returns(workflowDefinitions);

        // Act
        await _populator.PopulateAsync();

        // Assert
        await _notificationSenderMock.Received(1).SendAsync(
            Arg.Is<WorkflowDefinitionsReloaded>(n => n.ReloadedWorkflowDefinitions.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PopulateAsync_EnsuresActivityRegistryBeforeEachWorkflowDefinitionStorePopulation()
    {
        // Arrange
        _workflowDefinitionStorePopulatorMock
            .PopulateStoreAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WorkflowDefinition>());

        // Act
        await _populator.PopulateAsync();

        // Assert
        Received.InOrder(() =>
        {
            _activityRegistryPopulatorMock.EnsureRegistryPopulatedAsync(Arg.Any<CancellationToken>());
            _workflowDefinitionStorePopulatorMock.PopulateStoreAsync(false, Arg.Any<CancellationToken>());
            _activityRegistryPopulatorMock.EnsureRegistryPopulatedAsync(Arg.Any<CancellationToken>());
            _workflowDefinitionStorePopulatorMock.PopulateStoreAsync(true, Arg.Any<CancellationToken>());
            _notificationSenderMock.SendAsync(Arg.Any<WorkflowDefinitionsReloaded>(), Arg.Any<CancellationToken>());
        });

        await _activityRegistryPopulatorMock.Received(2).EnsureRegistryPopulatedAsync(Arg.Any<CancellationToken>());
    }
}
