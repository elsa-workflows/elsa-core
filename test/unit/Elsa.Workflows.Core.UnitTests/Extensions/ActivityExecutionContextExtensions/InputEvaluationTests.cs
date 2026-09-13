using Elsa.Testing.Shared;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class InputEvaluationTests
{
    private readonly ActivityWithSensitiveInputs _activity = new();
    private readonly ActivityTestFixture _fixture;

    public InputEvaluationTests()
    {
        _fixture = new(_activity);
    }

    [Test]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsSensitive_RemovesItFromActivityState()
    {
        _fixture.ConfigureContext(context => context.ActivityState[nameof(ActivityWithSensitiveInputs.SensitiveText)] = "stale-secret");

        var context = await ActivateAsync();

        await Assert.That(context.ActivityState.ContainsKey(nameof(ActivityWithSensitiveInputs.SensitiveText))).IsFalse();
        await Assert.That(_activity.CapturedSensitiveText).IsEqualTo("secret");
    }

    [Test]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsNotSensitive_StoresItInActivityState()
    {
        var context = await ActivateAsync();

        await Assert.That(context.ActivityState[nameof(ActivityWithSensitiveInputs.PublicText)]).IsEqualTo("public");
        await Assert.That(_activity.CapturedPublicText).IsEqualTo("public");
    }

    private Task<ActivityExecutionContext> ActivateAsync() => _fixture.ExecuteAsync();

    private sealed class ActivityWithSensitiveInputs : CodeActivity
    {
        [Input(CanContainSecrets = true)]
        public Input<string?> SensitiveText { get; set; } = new("secret");

        [Input]
        public Input<string?> PublicText { get; set; } = new("public");

        public string? CapturedSensitiveText { get; private set; }
        public string? CapturedPublicText { get; private set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            CapturedSensitiveText = context.Get(SensitiveText);
            CapturedPublicText = context.Get(PublicText);
        }
    }
}