using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Exceptions;

namespace Elsa.Activities.UnitTests.Primitives;

public class NotFoundActivityTests
{
    [Fact(DisplayName = "NotFoundActivity throws ActivityNotFoundException")]
    public async Task Should_Throw_ActivityNotFoundException()
    {
        // Arrange
        const string typeName = "MyMissingActivity";

        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName);
        Assert.Equal(typeName, exception.MissingTypeName);
    }

    [Fact(DisplayName = "NotFoundActivity includes type name in exception")]
    public async Task Should_Include_TypeName_In_Exception()
    {
        // Arrange
        const string typeName = "Custom.Namespace.MyActivity";

        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName);
        Assert.Contains(typeName, exception.Message);
    }

    [Fact(DisplayName = "NotFoundActivity includes version in exception")]
    public async Task Should_Include_Version_In_Exception()
    {
        // Arrange
        const string typeName = "MyMissingActivity";
        const int version = 2;

        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName, version);
        Assert.Equal(version, exception.MissingTypeVersion);
        Assert.Contains(version.ToString(), exception.Message);
    }

    [Theory(DisplayName = "NotFoundActivity preserves various type names")]
    [InlineData("SimpleActivity")]
    [InlineData("Namespace.Activity")]
    [InlineData("My.Custom.Namespace.ComplexActivity")]
    [InlineData("Activity123")]
    public async Task Should_Preserve_Various_TypeNames(string typeName)
    {
        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName);
        Assert.Equal(typeName, exception.MissingTypeName);
    }

    private static async Task<ActivityNotFoundException> ExecuteAndAssertExceptionAsync(string typeName, int version = 0)
    {
        var notFoundActivity = new NotFoundActivity(typeName, null, null)
        {
            MissingTypeVersion = version
        };
        return await Assert.ThrowsAsync<ActivityNotFoundException>(() => ExecuteAsync(notFoundActivity));
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }
}
