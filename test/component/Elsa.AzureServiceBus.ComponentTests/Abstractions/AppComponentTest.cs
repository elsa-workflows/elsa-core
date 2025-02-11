using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AzureServiceBus.ComponentTests;

[Collection(nameof(ServiceBusAppCollection))]
public abstract class AppComponentTest(App app) : IDisposable //IAsyncLifetime
{
    protected WorkflowServer WorkflowServer { get; } = app.WorkflowServer;
    protected Infrastructure Infrastructure { get; } = app.Infrastructure;
    protected IServiceScope Scope { get; private set; } = app.WorkflowServer.Services.CreateScope();

    public void Dispose()
    {
        // Disposing the Scope here and in other places where it is created somehow seems to cause the test runner to hang when running other test projects.
        // Let's comment it out for the time being.
        //Scope.Dispose();
        
        OnDispose();
    }
    
    protected virtual void OnDispose()
    {
    }
}