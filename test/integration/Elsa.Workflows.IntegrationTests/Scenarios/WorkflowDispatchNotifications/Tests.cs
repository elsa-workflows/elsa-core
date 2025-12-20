using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDispatchNotifications;

public class Tests
{
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly Spy _spy;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureServices(s =>
            {
                s.AddSingleton<Spy>();
                s.AddNotificationHandler<TestHandler, WorkflowDefinitionDispatching>();
                s.AddNotificationHandler<TestHandler, WorkflowDefinitionDispatched>();
                s.AddNotificationHandler<TestHandler, WorkflowInstanceDispatching>();
                s.AddNotificationHandler<TestHandler, WorkflowInstanceDispatched>();
            })
            .Build();
        
        _workflowDispatcher = services.GetRequiredService<IWorkflowDispatcher>();
        _spy = services.GetRequiredService<Spy>();
    }

    [Fact(DisplayName = "Dispatching workflow definition should emit notifications")]
    public async Task DispatchWorkflowDefinition_ShouldEmitNotifications()
    {
        // Arrange
        var definitionVersionId = "test-definition-version-id";
        var request = new DispatchWorkflowDefinitionRequest(definitionVersionId)
        {
            CorrelationId = "test-correlation-id",
            Input = new Dictionary<string, object> { { "TestKey", "TestValue" } }
        };

        // Act
        await _workflowDispatcher.DispatchAsync(request, null);

        // Allow async notification handlers to complete
        await Task.Delay(100);

        // Assert
        Assert.True(_spy.WorkflowDefinitionDispatchingWasCalled, "WorkflowDefinitionDispatching notification should be called");
        Assert.True(_spy.WorkflowDefinitionDispatchedWasCalled, "WorkflowDefinitionDispatched notification should be called");
        Assert.NotNull(_spy.CapturedDefinitionRequest);
        Assert.Equal(definitionVersionId, _spy.CapturedDefinitionRequest.DefinitionVersionId);
        Assert.Equal("test-correlation-id", _spy.CapturedDefinitionRequest.CorrelationId);
        Assert.NotNull(_spy.CapturedResponse);
        Assert.True(_spy.CapturedResponse.Succeeded);
    }

    [Fact(DisplayName = "Dispatching workflow instance should emit notifications")]
    public async Task DispatchWorkflowInstance_ShouldEmitNotifications()
    {
        // Arrange
        var instanceId = "test-instance-id";
        var request = new DispatchWorkflowInstanceRequest(instanceId)
        {
            CorrelationId = "test-correlation-id",
            Input = new Dictionary<string, object> { { "TestKey", "TestValue" } }
        };

        // Act
        await _workflowDispatcher.DispatchAsync(request, null);

        // Allow async notification handlers to complete
        await Task.Delay(100);

        // Assert
        Assert.True(_spy.WorkflowInstanceDispatchingWasCalled, "WorkflowInstanceDispatching notification should be called");
        Assert.True(_spy.WorkflowInstanceDispatchedWasCalled, "WorkflowInstanceDispatched notification should be called");
        Assert.NotNull(_spy.CapturedInstanceRequest);
        Assert.Equal(instanceId, _spy.CapturedInstanceRequest.InstanceId);
        Assert.Equal("test-correlation-id", _spy.CapturedInstanceRequest.CorrelationId);
        Assert.NotNull(_spy.CapturedResponse);
        Assert.True(_spy.CapturedResponse.Succeeded);
    }
}
