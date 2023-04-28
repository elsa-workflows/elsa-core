using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ActivityNotificationsMiddleware;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly Spy _spy;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureServices(s =>
            {
                s.AddSingleton<Spy>();
                s.AddNotificationHandler<TestHandler, ActivityExecuting>();
                s.AddNotificationHandler<TestHandler, ActivityExecuted>();
            })
            
            .Build();
        
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        _spy = services.GetRequiredService<Spy>();
    }

    [Fact(DisplayName = "Running workflow with activity notifications middleware results in notifications being published")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync<HelloWorldWorkflow>();
        Assert.True(_spy.ActivityExecutingWasCalled);
        Assert.True(_spy.ActivityExecutedWasCalled);
    }
}