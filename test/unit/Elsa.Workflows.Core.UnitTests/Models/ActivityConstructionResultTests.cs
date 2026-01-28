using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.Models;

public class ActivityConstructionResultTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Constructor_WithVaryingExceptionCounts_SetsHasExceptionsCorrectly(int exceptionCount, bool expectedHasExceptions)
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(exceptionCount);

        // Act
        var result = new ActivityConstructionResult(activity, exceptions);

        // Assert
        Assert.Same(activity, result.Activity);
        Assert.Equal(exceptionCount, result.Exceptions.Count());
        Assert.Equal(expectedHasExceptions, result.HasExceptions);
    }

    [Fact]
    public void Constructor_WithNullExceptions_TreatsAsEmpty()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        var result = new ActivityConstructionResult(activity, null);

        // Assert
        Assert.Empty(result.Exceptions);
        Assert.False(result.HasExceptions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Cast_PreservesActivityAndExceptions(int exceptionCount)
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(exceptionCount);
        var result = new ActivityConstructionResult(activity, exceptions);

        // Act
        var typedResult = result.Cast<WriteLine>();

        // Assert
        Assert.IsType<ActivityConstructionResult<WriteLine>>(typedResult);
        Assert.Same(activity, typedResult.Activity);
        Assert.Equal(exceptionCount, typedResult.Exceptions.Count());
        Assert.Equal(result.HasExceptions, typedResult.HasExceptions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GenericConstructor_CreatesTypedResult(int exceptionCount)
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(exceptionCount);

        // Act
        var result = new ActivityConstructionResult<WriteLine>(activity, exceptions);

        // Assert
        Assert.Same(activity, result.Activity);
        Assert.Equal(exceptionCount, result.Exceptions.Count());
        Assert.Equal(exceptionCount > 0, result.HasExceptions);
        Assert.IsAssignableFrom<ActivityConstructionResult>(result);
    }

    [Fact]
    public void Exceptions_CanBeEnumerated()
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(3);
        var result = new ActivityConstructionResult(activity, exceptions);

        // Act
        var enumeratedExceptions = new List<Exception>();
        foreach (var ex in result.Exceptions)
        {
            enumeratedExceptions.Add(ex);
        }

        // Assert
        Assert.Equal(3, enumeratedExceptions.Count);
        Assert.All(enumeratedExceptions, ex => Assert.NotNull(ex));
    }

    // Helper methods
    private static WriteLine CreateActivity() => new("test");

    private static List<Exception>? CreateExceptions(int count)
    {
        if (count == 0) return null;

        var exceptions = new List<Exception>();
        for (var i = 0; i < count; i++)
        {
            exceptions.Add(new InvalidOperationException($"Error {i + 1}"));
        }
        return exceptions;
    }
}
