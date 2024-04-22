using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.ComponentTests;

[Collection(nameof(WebAppCollection))]
public abstract class ComponentTest : IDisposable
{
    protected ComponentTest(ITestOutputHelper testOutputHelper, WorkflowServerWebAppFactoryFixture factoryFixture)
    {
        factoryFixture.TestOutputHelper = testOutputHelper;
        FactoryFixture = factoryFixture;
        Scope = factoryFixture.Services.CreateScope();
    }

    protected WorkflowServerWebAppFactoryFixture FactoryFixture { get; }
    protected IServiceScope Scope { get; }
    
    protected virtual void OnDispose()
    {
    }

    void IDisposable.Dispose()
    {
        Scope.Dispose();
        OnDispose();
    }
}