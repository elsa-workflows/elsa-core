using System.Linq;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;
using Elsa.Modules.Activities.Contracts;
using Elsa.Modules.Activities.Providers;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Workflows;

public class HelloWorldWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public HelloWorldWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var combinedTextWriter = new CombinedTextWriter(_capturingTextWriter, new XunitConsoleTextWriter(testOutputHelper));
        var services = TestContainerBuilder.Build(testOutputHelper, x => x.AddSingleton<IStandardOutStreamProvider>(new StandardOutStreamProvider(combinedTextWriter)));
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