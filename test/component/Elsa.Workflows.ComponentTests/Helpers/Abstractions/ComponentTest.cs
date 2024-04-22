using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests;

[Collection(nameof(WebAppCollection))]
public abstract class ComponentTest(WorkflowServerWebAppFactoryFixture factoryFixture) : IDisposable
{
    protected WorkflowServerWebAppFactoryFixture FactoryFixture { get; } = factoryFixture;
    protected IServiceScope Scope { get; } = factoryFixture.Services.CreateScope();

    protected virtual void OnDispose()
    {
    }

    void IDisposable.Dispose()
    {
        Scope.Dispose();
        OnDispose();
    }
}