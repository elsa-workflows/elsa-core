using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptNativeVariables;

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

    [Fact(DisplayName = "The JavaScript activity can access and modify native variables")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<JavaScriptWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var line1 = _capturingTextWriter.Lines.ToList().ElementAt(0);
        var line2 = _capturingTextWriter.Lines.ToList().ElementAt(1);
        var line3 = _capturingTextWriter.Lines.ToList().ElementAt(2);
        Assert.Equal("Jane Doe", line1);
        Assert.Equal("Apple, Banana, Orange", line2);
        Assert.Equal("jane.doe@acme.com", line3);
    }
}