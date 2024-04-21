using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.Api.ComponentTests;

[Collection(nameof(WebAppCollection))]
public abstract class ComponentTest
{
    protected ComponentTest(ITestOutputHelper testOutputHelper, WorkflowServerWebAppFactoryFixture factoryFixture)
    {
        factoryFixture.TestOutputHelper = testOutputHelper;
        FactoryFixture = factoryFixture;
        Scope = factoryFixture.Services.CreateScope();
    }

    protected WorkflowServerWebAppFactoryFixture FactoryFixture { get; }
    protected IServiceScope Scope { get; }
}