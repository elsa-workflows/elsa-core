using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.CompositesPassingData;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseJavaScript())
            .Build();
        _workflowBuilderFactory = services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "The main workflow can capture the result of the composite activity")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<AddTextMainWorkflow>();

        // Start workflow.
        await _workflowRunner.RunAsync(workflow);

        // Verify expected output.
        var line = _capturingTextWriter.Lines.ToList().Last();
        Assert.Equal("hi there obi wan", line);
    }
}