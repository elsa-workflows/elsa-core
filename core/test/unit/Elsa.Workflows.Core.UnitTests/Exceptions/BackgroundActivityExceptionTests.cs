using Elsa.Workflows.Exceptions;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Core.UnitTests.Exceptions;

public class BackgroundActivityExceptionTests
{
    [Fact]
    public void FromException_ReturnsTheCapturedBackgroundExceptionState()
    {
        var capturedState = new ExceptionState(typeof(InvalidOperationException), "Background work failed.", "background stack", null);
        var exception = new BackgroundActivityException(capturedState);

        var state = ExceptionState.FromException(exception);

        Assert.Same(capturedState, state);
        Assert.Equal("Background work failed.", exception.Message);
        Assert.Equal("background stack", exception.StackTrace);
    }
}
