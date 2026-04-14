using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class WriteLineTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "WriteLine prints the expected line to the console.")]
    public async Task Test1()
    {
        const string expectedLine = "Hello world!";
        var writeLine = new WriteLine(expectedLine);
        await _fixture.RunActivityAsync(writeLine);
        Assert.Equal(expectedLine, _fixture.CapturingTextWriter.Lines.Single());
    }
}