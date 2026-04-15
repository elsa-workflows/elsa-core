using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class SetNameTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "SetName sets the workflow instance name.")]
    public async Task Test1()
    {
        const string expectedName = "Foo";
        var setName = new SetName(new Input<string>(expectedName));
        var result = await _fixture.RunActivityAsync(setName);
        var actualName = result.WorkflowState.Name;
        Assert.Equal(expectedName, actualName);
    }
}