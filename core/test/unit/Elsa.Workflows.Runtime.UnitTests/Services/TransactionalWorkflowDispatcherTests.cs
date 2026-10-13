using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class TransactionalWorkflowDispatcherTests
{
    private readonly IWorkflowDispatcher _innerDispatcher = Substitute.For<IWorkflowDispatcher>();
    private readonly IWorkflowDispatchOutbox _outbox = Substitute.For<IWorkflowDispatchOutbox>();
    private readonly IWorkflowDispatchOutboxAccessor _outboxAccessor = Substitute.For<IWorkflowDispatchOutboxAccessor>();
    private readonly IIdentityGenerator _identityGenerator = Substitute.For<IIdentityGenerator>();

    [Fact]
    public async Task DispatchAsync_PersistsChannelOnEnqueuedItem()
    {
        var dispatcher = await CreateDispatcherInsideWorkflowAsync();

        await dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest("definition-version-1"), new DispatchWorkflowOptions { Channel = "priority" });

        await _outbox.Received(1).EnqueueAsync(
            Arg.Any<WorkflowExecutionContext>(),
            Arg.Is<WorkflowDispatchOutboxItem>(x => x.Channel == "priority" && x.Kind == WorkflowDispatchOutboxItemKind.WorkflowDefinition),
            Arg.Any<CancellationToken>());
        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
    }

    [Fact]
    public async Task DispatchAsync_FixesChildInstanceIdAndRequestsIdempotentCreation()
    {
        _identityGenerator.GenerateId().Returns("child-1");
        var dispatcher = await CreateDispatcherInsideWorkflowAsync();

        await dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest("definition-version-1"));

        await _outbox.Received(1).EnqueueAsync(
            Arg.Any<WorkflowExecutionContext>(),
            Arg.Is<WorkflowDispatchOutboxItem>(x => x.WorkflowDefinitionCommand!.InstanceId == "child-1" && x.WorkflowDefinitionCommand.SkipIfInstanceExists),
            Arg.Any<CancellationToken>());
    }

    private async Task<TransactionalWorkflowDispatcher> CreateDispatcherInsideWorkflowAsync()
    {
        var fixture = await new ActivityTestFixture(new Sequence()).BuildAsync();
        _outboxAccessor.WorkflowExecutionContext.Returns(fixture.WorkflowExecutionContext);
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IWorkflowDispatchOutbox)).Returns(_outbox);

        return new(
            _innerDispatcher,
            serviceProvider,
            _outboxAccessor,
            _identityGenerator,
            Microsoft.Extensions.Options.Options.Create(new WorkflowDispatcherOptions { UseTransactionalOutbox = true }));
    }
}
