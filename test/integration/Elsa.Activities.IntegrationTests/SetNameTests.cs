using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class SetNameTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _serviceProvider;

    public SetNameTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }
    
    [Fact(DisplayName = "WriteLine prints the expected line to the console.")]
    public async Task Test1()
    {
        const string expectedName = "Foo";
        var setName = new SetName(new Input<string>(expectedName));
        var result = await _serviceProvider.RunActivityAsync(setName);
        var actualName = result.WorkflowState.Properties[SetName.WorkflowInstanceNameKey];
        Assert.Equal(expectedName, actualName);
    }
}