using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task ObsoleteConstructor_StillEnqueues()
    {
        await CreateDispatcherInsideWorkflowAsync();
#pragma warning disable CS0618
        var dispatcher = new TransactionalWorkflowDispatcher(_innerDispatcher, _serviceProvider, _outboxAccessor, Substitute.For<INotificationSender>(), _identityGenerator,
            Microsoft.Extensions.Options.Options.Create(new WorkflowDispatcherOptions { UseTransactionalOutbox = true }));
#pragma warning restore CS0618

        await dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest("definition-version-1"));

        await _outbox.Received(1).EnqueueAsync(Arg.Any<WorkflowExecutionContext>(), Arg.Any<WorkflowDispatchOutboxItem>(), Arg.Any<CancellationToken>());
        await _innerDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, default, default);
    }

    [Fact]
    public void ActivatorUtilities_PicksCurrentConstructor()
    {
        var services = new ServiceCollection()
            .AddSingleton(_outboxAccessor)
            .AddSingleton(_identityGenerator)
            .AddSingleton(Substitute.For<INotificationSender>())
            .AddOptions()
            .BuildServiceProvider();

        var dispatcher = ActivatorUtilities.CreateInstance<TransactionalWorkflowDispatcher>(services, _innerDispatcher);

        Assert.NotNull(dispatcher);
    }

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private async Task<TransactionalWorkflowDispatcher> CreateDispatcherInsideWorkflowAsync()
    {
        var fixture = await new ActivityTestFixture(new Sequence()).BuildAsync();
        _outboxAccessor.WorkflowExecutionContext.Returns(fixture.WorkflowExecutionContext);
        _serviceProvider.GetService(typeof(IWorkflowDispatchOutbox)).Returns(_outbox);

        return new(
            _innerDispatcher,
            _serviceProvider,
            _outboxAccessor,
            _identityGenerator,
            Microsoft.Extensions.Options.Options.Create(new WorkflowDispatcherOptions { UseTransactionalOutbox = true }));
    }
}
