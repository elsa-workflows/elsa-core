using Elsa.Workflows.Exceptions;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConversionExceptionStateTests
{
    [Fact]
    public void FromException_PersistsOnlyStructuredSafeMetadata()
    {
        var exception = new OutputConversionException(
            "sample.to-text",
            OutputConversionFailureStage.Invocation,
            "activity-1",
            "TestActivity",
            "Result",
            "workflow-result",
            typeof(int),
            typeof(string),
            new InvalidOperationException("sensitive converter detail"));

        var state = ExceptionState.FromException(exception)!;
        var persistedText = string.Join(" ", state.Metadata!.Values.Append(state.Message));

        Assert.Equal("sample.to-text", state.Metadata[nameof(OutputConversionException.ConverterId)]);
        Assert.Equal("Invocation", state.Metadata[nameof(OutputConversionException.Stage)]);
        Assert.Equal("activity-1", state.Metadata[nameof(OutputConversionException.ActivityId)]);
        Assert.Equal("Result", state.Metadata[nameof(OutputConversionException.OutputName)]);
        Assert.Null(state.InnerException);
        Assert.DoesNotContain("sensitive converter detail", persistedText);
        Assert.DoesNotContain("settings", persistedText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("value", persistedText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromException_DoesNotPersistArbitraryExceptionData()
    {
        var exception = new InvalidOperationException("failure");
        exception.Data["secret"] = "do not persist";

        var state = ExceptionState.FromException(exception)!;

        Assert.Null(state.Metadata);
        Assert.DoesNotContain("do not persist", state.Message);
    }
}
