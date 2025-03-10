using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class WriteLineTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public WriteLineTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Run a simple workflow")]
    public async Task Test1()
    {
        var expectedOutput = "Hello World!";
        var workflow = Workflow.FromActivity(new WriteLine(expectedOutput));
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync(workflow);
        var line = _capturingTextWriter.Lines.FirstOrDefault();
        Assert.Equal(expectedOutput, line);
    }
}