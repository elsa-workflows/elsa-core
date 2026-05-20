using System.Reflection;
using System.Text.Json;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class ActivityFactoryTests
{
    // Regression coverage for elsa-workflows/elsa-core#6430.
    // When a workflow is resumed after a host restart and the activity registry
    // is not fully warm, ActivityJsonConverter's Find<NotFoundActivity>()!
    // fallback can return null. The null flows into ActivityFactory as
    // context.ActivityDescriptor and the synthetic input/output readers must
    // guard against it instead of NRE'ing on .Inputs / .Outputs.

    private static readonly MethodInfo ReadSyntheticInputsMethod =
        typeof(ActivityFactory).GetMethod("ReadSyntheticInputs", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ReadSyntheticOutputsMethod =
        typeof(ActivityFactory).GetMethod("ReadSyntheticOutputs", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact]
    public void ReadSyntheticInputs_DoesNotThrow_WhenActivityDescriptorIsNull()
    {
        var factory = new ActivityFactory();
        var element = JsonDocument.Parse("{}").RootElement;
        var activity = new End();

        var ex = Record.Exception(() =>
            ReadSyntheticInputsMethod.Invoke(factory, new object?[] { null, activity, element, new JsonSerializerOptions() }));

        Assert.Null(ex);
    }

    [Fact]
    public void ReadSyntheticOutputs_DoesNotThrow_WhenActivityDescriptorIsNull()
    {
        var factory = new ActivityFactory();
        var element = JsonDocument.Parse("{}").RootElement;
        var activity = new End();

        var ex = Record.Exception(() =>
            ReadSyntheticOutputsMethod.Invoke(factory, new object?[] { null, activity, element }));

        Assert.Null(ex);
    }
}
