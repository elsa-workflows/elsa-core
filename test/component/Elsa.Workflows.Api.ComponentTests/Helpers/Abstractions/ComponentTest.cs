using Elsa.Workflows.Api.ComponentTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.Api.ComponentTests.Helpers;

public abstract class ComponentTest : IClassFixture<WorkflowServerTestWebAppFactory>
{
    protected ComponentTest(ITestOutputHelper testOutputHelper, WorkflowServerTestWebAppFactory factory)
    {
        factory.TestOutputHelper = testOutputHelper;
        Factory = factory;
        Scope = factory.Services.CreateScope();
    }

    protected WorkflowServerTestWebAppFactory Factory { get; }
    protected IServiceScope Scope { get; }
}