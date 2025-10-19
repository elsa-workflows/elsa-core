using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class ResultTests
{

    [Fact]
    public async Task SetResult_SetsResultProperty_WhenActivityImplementsIActivityWithResult()
    {
        // Arrange
        var activity = new ActivityWithResult();
        var context = await CreateContextAsync(activity);
        var testValue = "ResultValue";

        // Act
        context.SetResult(testValue);

        // Assert
        var result = context.Get(activity.Result);
        Assert.Equal(testValue, result);
    }

    [Fact]
    public async Task SetResult_ThrowsException_WhenActivityDoesNotImplementIActivityWithResult()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act & Assert
        Assert.Throws<Exception>(() => context.SetResult("value"));
    }

    private class ActivityWithResult : Activity, IActivityWithResult
    {
        public Output? Result { get; set; } = new Output<object?>();
    }
}
