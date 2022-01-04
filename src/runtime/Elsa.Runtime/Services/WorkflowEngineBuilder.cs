using Elsa.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime.Services;

public class WorkflowEngineBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();
        
    public IWorkflowServer BuildWorkflowServer()
    {
        var serviceProvider = Services.BuildServiceProvider();
        return new WorkflowServer(serviceProvider);
    }
}