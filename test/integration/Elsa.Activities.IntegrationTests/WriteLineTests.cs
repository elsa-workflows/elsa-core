using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests;

public class WriteLineTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("WriteLine prints the expected line to the console.")]
    public async Task Test1()
    {
        const string expectedLine = "Hello world!";
        var writeLine = new WriteLine(expectedLine);
        await _fixture.RunActivityAsync(writeLine);
        await Assert.That(_fixture.CapturingTextWriter.Lines.Single()).IsEqualTo(expectedLine);

    }
}
