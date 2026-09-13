using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Activities.IntegrationTests;

public class SetNameTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("SetName sets the workflow instance name.")]
    public async Task Test1()
    {
        const string expectedName = "Foo";
        var setName = new SetName(new Input<string>(expectedName));
        var result = await _fixture.RunActivityAsync(setName);
        var actualName = result.WorkflowState.Name;
        await Assert.That(actualName).IsEqualTo(expectedName);

    }
}
