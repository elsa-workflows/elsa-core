using Elsa.Workflows.Exceptions;
using Elsa.Workflows.State;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConversionExceptionStateTests
{
    [Test]
    public async Task FromException_PersistsOnlyStructuredSafeMetadata()
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

        await Assert.That(state.Metadata[nameof(OutputConversionException.ConverterId)]).IsEqualTo("sample.to-text");
        await Assert.That(state.Metadata[nameof(OutputConversionException.Stage)]).IsEqualTo("Invocation");
        await Assert.That(state.Metadata[nameof(OutputConversionException.ActivityId)]).IsEqualTo("activity-1");
        await Assert.That(state.Metadata[nameof(OutputConversionException.OutputName)]).IsEqualTo("Result");
        await Assert.That(state.InnerException).IsNull();
        await Assert.That(persistedText).DoesNotContain("sensitive converter detail");
        await Assert.That(persistedText).DoesNotContain("settings");
        await Assert.That(persistedText).DoesNotContain("value");
    }

    [Test]
    public async Task FromException_DoesNotPersistArbitraryExceptionData()
    {
        var exception = new InvalidOperationException("failure");
        exception.Data["secret"] = "do not persist";

        var state = ExceptionState.FromException(exception)!;

        await Assert.That(state.Metadata).IsNull();
        await Assert.That(state.Message).DoesNotContain("do not persist");
    }

    [Test]
    public async Task Metadata_PreservesTheOriginalFourPositionRecordContract()
    {
        var state = new ExceptionState(typeof(InvalidOperationException), "failure", "stack", null)
        {
            Metadata = new Dictionary<string, string> { ["stage"] = "Invocation" }
        };

        var (type, message, stackTrace, innerException) = state;

        await Assert.That(type).IsEqualTo(typeof(InvalidOperationException));
        await Assert.That(message).IsEqualTo("failure");
        await Assert.That(stackTrace).IsEqualTo("stack");
        await Assert.That(innerException).IsNull();
        await Assert.That(state.Metadata!["stage"]).IsEqualTo("Invocation");
    }
}