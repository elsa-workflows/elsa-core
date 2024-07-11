using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class WriteLineTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _serviceProvider;

    public WriteLineTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }
    
    [Fact(DisplayName = "WriteLine prints the expected line to the console.")]
    public async Task Test1()
    {
        const string expectedLine = "Hello world!";
        var writeLine = new WriteLine(expectedLine);
        await _serviceProvider.RunActivityAsync(writeLine);
        Assert.Equal(expectedLine, _capturingTextWriter.Lines.Single());
    }
}