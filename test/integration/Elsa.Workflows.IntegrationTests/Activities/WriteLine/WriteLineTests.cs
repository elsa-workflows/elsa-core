using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class WriteLineTests : IAsyncDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public WriteLineTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Test]
    [DisplayName("Run a simple workflow")]
    public async Task Test1()
    {
        var expectedOutput = "Hello World!";
        var workflow = Workflow.FromActivity(new WriteLine(expectedOutput));
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync(workflow);
        var line = _capturingTextWriter.Lines.FirstOrDefault();
        await Assert.That(line).IsEqualTo(expectedOutput);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
