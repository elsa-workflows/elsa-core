using Elsa.Testing.Shared;
using Elsa.Workflows.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityNotificationsMiddleware;

public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly Spy _spy;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureServices(s =>
            {
                s.AddSingleton<Spy>();
                s.AddNotificationHandler<TestHandler, ActivityExecuting>();
                s.AddNotificationHandler<TestHandler, ActivityExecuted>();
            })
            
            .Build();
        
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
        _spy = _services.GetRequiredService<Spy>();
    }

    [Test]
    [DisplayName("Running workflow with activity notifications middleware results in notifications being published")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync<HelloWorldWorkflow>();
        await Assert.That(_spy.ActivityExecutingWasCalled).IsTrue();
        await Assert.That(_spy.ActivityExecutedWasCalled).IsTrue();
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
