using Elsa.Common;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

/// <summary>
/// Shared scaffolding for <see cref="DrainOrchestrator"/> unit tests: builds the mock dependency graph and exposes
/// hooks the test classes can manipulate.
/// </summary>
public abstract class DrainOrchestratorTestsBase
{
    protected readonly ISystemClock Clock = Substitute.For<ISystemClock>();
    protected readonly IQuiescenceSignal Signal = Substitute.For<IQuiescenceSignal>();
    protected readonly IIngressSourceRegistry Registry = Substitute.For<IIngressSourceRegistry>();
    protected readonly IExecutionCycleRegistry ExecutionCycleRegistry = Substitute.For<IExecutionCycleRegistry>();
    protected readonly IWorkflowInstanceStore InstanceStore = Substitute.For<IWorkflowInstanceStore>();
    protected readonly IWorkflowExecutionLogStore LogStore = Substitute.For<IWorkflowExecutionLogStore>();
    protected readonly GracefulShutdownOptions Options = new();
    protected readonly HostOptions HostOptionsValue = new() { ShutdownTimeout = TimeSpan.FromSeconds(60) };
    private DateTimeOffset _now = DateTimeOffset.Parse("2026-04-25T10:00:00Z");

    protected DrainOrchestratorTestsBase()
    {
        Clock.UtcNow.Returns(_ => _now);
        Signal.CurrentState.Returns(QuiescenceState.Initial("gen-1"));
        Signal.BeginDrainAsync(Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<QuiescenceState>(QuiescenceState.Initial("gen-1")));
        Registry.Sources.Returns(Array.Empty<IIngressSource>());
        Registry.Snapshot().Returns(Array.Empty<IngressSourceSnapshot>());
        ExecutionCycleRegistry.ActiveCount.Returns(0);
        ExecutionCycleRegistry.ListActiveCycles().Returns(Array.Empty<ExecutionCycleHandle>());
    }

    protected void AdvanceTime(TimeSpan span) => _now += span;

    protected void SetTime(DateTimeOffset value) => _now = value;

    protected DrainOrchestrator BuildSut()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IWorkflowInstanceStore)).Returns(InstanceStore);
        sp.GetService(typeof(IWorkflowExecutionLogStore)).Returns(LogStore);
        scope.ServiceProvider.Returns(sp);
        scopeFactory.CreateScope().Returns(scope);

        var identityGenerator = Substitute.For<IIdentityGenerator>();
        identityGenerator.GenerateId().Returns(_ => Guid.NewGuid().ToString("N"));

        return new DrainOrchestrator(
            Signal,
            Registry,
            ExecutionCycleRegistry,
            scopeFactory,
            Microsoft.Extensions.Options.Options.Create(Options),
            Microsoft.Extensions.Options.Options.Create(HostOptionsValue),
            Clock,
            identityGenerator,
            Substitute.For<ILogger<DrainOrchestrator>>());
    }

    protected static IIngressSource CreateSource(string name, TimeSpan? pauseTimeout = null, Func<CancellationToken, ValueTask>? pauseImpl = null)
    {
        var source = Substitute.For<IIngressSource>();
        source.Name.Returns(name);
        source.PauseTimeout.Returns(pauseTimeout ?? TimeSpan.FromSeconds(5));
        source.CurrentState.Returns(IngressSourceState.Running);
        if (pauseImpl is not null)
            source.PauseAsync(Arg.Any<CancellationToken>()).Returns(ci => pauseImpl(ci.Arg<CancellationToken>()));
        else
            source.PauseAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        return source;
    }
}
