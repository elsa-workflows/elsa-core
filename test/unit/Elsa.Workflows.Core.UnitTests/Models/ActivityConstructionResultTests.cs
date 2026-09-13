using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Models;

public class ActivityConstructionResultTests
{
    [Test]
    [Arguments(0, false)]
    [Arguments(1, true)]
    [Arguments(2, true)]
    public async Task Constructor_WithVaryingExceptionCounts_SetsPropertiesCorrectly(int exceptionCount, bool expectedHasExceptions)
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(exceptionCount);

        // Act
        var result = new ActivityConstructionResult(activity, exceptions);

        // Assert
        await Assert.That(result.Activity).IsSameReferenceAs(activity);
        await Assert.That(result.Exceptions.Count()).IsEqualTo(exceptionCount);
        await Assert.That(result.HasExceptions).IsEqualTo(expectedHasExceptions);
    }

    [Test]
    public async Task Constructor_WithNullExceptions_TreatsAsEmpty()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        var result = new ActivityConstructionResult(activity, null);

        // Assert
        await Assert.That(result.Exceptions).IsEmpty();
        await Assert.That(result.HasExceptions).IsFalse();
    }

    [Test]
    [Arguments(0, false)]
    [Arguments(1, true)]
    [Arguments(3, true)]
    public async Task Cast_PreservesActivityAndExceptions(int exceptionCount, bool expectedHasExceptions)
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(exceptionCount);
        var result = new ActivityConstructionResult(activity, exceptions);

        // Act
        var typedResult = result.Cast<WriteLine>();

        // Assert
        await Assert.That(typedResult).IsTypeOf<ActivityConstructionResult<WriteLine>>();
        await Assert.That(typedResult.Activity).IsSameReferenceAs(activity);
        await Assert.That(typedResult.Exceptions.Count()).IsEqualTo(exceptionCount);
        await Assert.That(typedResult.HasExceptions).IsEqualTo(expectedHasExceptions);
    }

    [Test]
    [Arguments(0, false)]
    [Arguments(1, true)]
    [Arguments(2, true)]
    public async Task GenericConstructor_CreatesTypedResultWithInheritance(int exceptionCount, bool expectedHasExceptions)
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(exceptionCount);

        // Act
        var result = new ActivityConstructionResult<WriteLine>(activity, exceptions);

        // Assert
        await Assert.That(result.Activity).IsSameReferenceAs(activity);
        await Assert.That(result.Exceptions.Count()).IsEqualTo(exceptionCount);
        await Assert.That(result.HasExceptions).IsEqualTo(expectedHasExceptions);
        await Assert.That(result).IsAssignableTo<ActivityConstructionResult>();
    }

    [Test]
    public async Task Exceptions_CanBeEnumerated()
    {
        // Arrange
        var activity = CreateActivity();
        var exceptions = CreateExceptions(3);
        var result = new ActivityConstructionResult(activity, exceptions);

        // Act & Assert
        var count = 0;
        foreach (var ex in result.Exceptions)
        {
            await Assert.That(ex).IsNotNull();
            count++;
        }
        await Assert.That(count).IsEqualTo(3);
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