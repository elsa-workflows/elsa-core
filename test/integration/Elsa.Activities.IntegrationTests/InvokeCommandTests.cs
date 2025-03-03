using Elsa.CLI.Activities;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class InvokeCommandTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact]
    public async Task Test1()
    {
        const string expectedLine = "Hello world!";

        var command = new InvokeCommand();

        // TODO: Write tests

        await _serviceProvider.RunActivityAsync(command);
    }
}