using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptUnderscoreVariables;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseJavaScript())
            .Build();
        _workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "A variable whose name contains an underscore is accessible from JavaScript")]
    public async Task UnderscoreVariableIsAccessible()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<UnderscoreVariableWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        Assert.Equal("Cliënt gevallen", _capturingTextWriter.Lines.ToList().ElementAt(0));
    }
}
