using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class InvokeCommandTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact]
    public async Task Test1()
    {
        // TODO: Write tests
    }
}