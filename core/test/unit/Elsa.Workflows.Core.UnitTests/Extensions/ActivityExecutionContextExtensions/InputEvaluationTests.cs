using Elsa.Testing.Shared;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class InputEvaluationTests
{
    private readonly ActivityWithSensitiveInputs _activity = new();
    private readonly ActivityTestFixture _fixture;

    public InputEvaluationTests()
    {
        _fixture = new(_activity);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsSensitive_RemovesItFromActivityState()
    {
        _fixture.ConfigureContext(context => context.ActivityState[nameof(ActivityWithSensitiveInputs.SensitiveText)] = "stale-secret");

        var context = await ActivateAsync();

        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithSensitiveInputs.SensitiveText)));
        Assert.Equal("secret", _activity.CapturedSensitiveText);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsNotSensitive_StoresItInActivityState()
    {
        var context = await ActivateAsync();

        Assert.Equal("public", context.ActivityState[nameof(ActivityWithSensitiveInputs.PublicText)]);
        Assert.Equal("public", _activity.CapturedPublicText);
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
