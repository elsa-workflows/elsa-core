using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.ComponentTests;

public abstract class IntegrationTest(WorkflowServerTestWebAppFactory factory) : IClassFixture<WorkflowServerTestWebAppFactory>
{
    protected WorkflowServerTestWebAppFactory Factory { get; } = factory;
    protected IServiceScope Scope { get; } = factory.Services.CreateScope();
}