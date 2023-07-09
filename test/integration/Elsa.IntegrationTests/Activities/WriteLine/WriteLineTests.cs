using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities;

public class WriteLineTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public WriteLineTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Run a simple workflow")]
    public async Task Test1()
    {
        var expectedOutput = "Hello World!";
        var workflow = Workflow.FromActivity(new WriteLine(expectedOutput));
        await _workflowRunner.RunAsync(workflow);
        var line = _capturingTextWriter.Lines.FirstOrDefault();
        Assert.Equal(expectedOutput, line);
    }
}