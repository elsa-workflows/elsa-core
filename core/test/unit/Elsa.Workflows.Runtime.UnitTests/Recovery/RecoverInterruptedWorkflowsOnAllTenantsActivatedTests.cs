using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.StartupTasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RuntimeFeature = Elsa.Workflows.Runtime.Features.WorkflowRuntimeFeature;
using ShellRuntimeFeature = Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature;

namespace Elsa.Workflows.Runtime.UnitTests.Recovery;

public class RecoverInterruptedWorkflowsOnAllTenantsActivatedTests
{
    [Fact(DisplayName = "Tenants activated runs the recovery startup task once through the task executor")]
    public async Task RunsStartupTaskThroughTaskExecutor()
    {
        var taskExecutor = Substitute.For<ITaskExecutor>();
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<IInterruptedRecoveryScanner>())
            .AddSingleton(taskExecutor)
            .BuildServiceProvider();
        var sut = new RecoverInterruptedWorkflowsOnAllTenantsActivated(services.GetRequiredService<IServiceScopeFactory>());

        await sut.AllTenantsActivatedAsync(CancellationToken.None);

        await taskExecutor.Received(1).ExecuteTaskAsync(Arg.Any<RecoverInterruptedWorkflowsStartupTask>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "The module and shell features register the handler and no per-tenant recovery startup task")]
    public void FeaturesRegisterHandlerOnly()
    {
        var shellServices = new ServiceCollection();
        new ShellRuntimeFeature().ConfigureServices(shellServices);

        var moduleServices = new ServiceCollection();
        var module = Substitute.For<IModule>();
        module.Services.Returns(moduleServices);
        new RuntimeFeature(module).Apply();

        foreach (var services in new[] { shellServices, moduleServices })
        {
            Assert.DoesNotContain(services, x => x.ServiceType == typeof(IStartupTask) && x.ImplementationType == typeof(RecoverInterruptedWorkflowsStartupTask));
            Assert.Single(services, x => x.ServiceType == typeof(IAllTenantsActivatedEvent) && x.ImplementationType == typeof(RecoverInterruptedWorkflowsOnAllTenantsActivated));
        }
    }
}
