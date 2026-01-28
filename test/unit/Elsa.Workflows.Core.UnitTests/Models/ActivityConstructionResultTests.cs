using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.Models;

public class ActivityConstructionResultTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Constructor_WithVaryingExceptionCounts_SetsPropertiesCorrectly(int exceptionCount, bool expectedHasExceptions)
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
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    public void Cast_PreservesActivityAndExceptions(int exceptionCount, bool expectedHasExceptions)
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
        Assert.Equal(expectedHasExceptions, typedResult.HasExceptions);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void GenericConstructor_CreatesTypedResultWithInheritance(int exceptionCount, bool expectedHasExceptions)
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(exceptionCount);

        // Act
        var result = new ActivityConstructionResult<WriteLine>(activity, exceptions);

        // Assert
        Assert.Same(activity, result.Activity);
        Assert.Equal(exceptionCount, result.Exceptions.Count());
        Assert.Equal(expectedHasExceptions, result.HasExceptions);
        Assert.IsAssignableFrom<ActivityConstructionResult>(result);
    }

    [Fact]
    public void Exceptions_CanBeEnumerated()
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(3);
        var result = new ActivityConstructionResult(activity, exceptions);

        // Act & Assert
        var count = 0;
        foreach (var ex in result.Exceptions)
        {
            Assert.NotNull(ex);
            count++;
        }
        Assert.Equal(3, count);
    }

    // Helper methods
    private static WriteLine CreateActivity() => new("test");

    private static List<Exception>? CreateExceptions(int count)
    {
        if (count == 0) return null;

        return Enumerable.Range(1, count)
            .Select(i => new InvalidOperationException($"Error {i}") as Exception)
            .ToList();
    }
}
