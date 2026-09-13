using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDispatchNotifications;

public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly Spy _spy;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureServices(s =>
            {
                s.AddSingleton<Spy>();
                s.AddNotificationHandler<TestHandler, WorkflowDefinitionDispatching>();
                s.AddNotificationHandler<TestHandler, WorkflowDefinitionDispatched>();
                s.AddNotificationHandler<TestHandler, WorkflowInstanceDispatching>();
                s.AddNotificationHandler<TestHandler, WorkflowInstanceDispatched>();
            })
            .Build();
        
        _workflowDispatcher = _services.GetRequiredService<IWorkflowDispatcher>();
        _spy = _services.GetRequiredService<Spy>();
    }

    [Test]
    [DisplayName("Dispatching workflow definition should emit notifications")]
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

        // Assert
        await Assert.That(_spy.WorkflowDefinitionDispatchingWasCalled).IsTrue().Because("WorkflowDefinitionDispatching notification should be called");
        await Assert.That(_spy.WorkflowDefinitionDispatchedWasCalled).IsTrue().Because("WorkflowDefinitionDispatched notification should be called");
        var capturedRequest = await Assert.That(_spy.CapturedDefinitionRequest).IsNotNull();
        await Assert.That(capturedRequest.DefinitionVersionId).IsEqualTo(definitionVersionId);
        await Assert.That(capturedRequest.CorrelationId).IsEqualTo("test-correlation-id");
        var capturedResponse = await Assert.That(_spy.CapturedResponse).IsNotNull();
        await Assert.That(capturedResponse.Succeeded).IsTrue();
    }

    [Test]
    [DisplayName("Dispatching workflow instance should emit notifications")]
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

        // Assert
        await Assert.That(_spy.WorkflowInstanceDispatchingWasCalled).IsTrue().Because("WorkflowInstanceDispatching notification should be called");
        await Assert.That(_spy.WorkflowInstanceDispatchedWasCalled).IsTrue().Because("WorkflowInstanceDispatched notification should be called");
        var capturedRequest = await Assert.That(_spy.CapturedInstanceRequest).IsNotNull();
        await Assert.That(capturedRequest.InstanceId).IsEqualTo(instanceId);
        await Assert.That(capturedRequest.CorrelationId).IsEqualTo("test-correlation-id");
        var capturedResponse = await Assert.That(_spy.CapturedResponse).IsNotNull();
        await Assert.That(capturedResponse.Succeeded).IsTrue();
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
