using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Exceptions;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Primitives;

public class NotFoundActivityTests
{
    [Test]
    [DisplayName("NotFoundActivity throws ActivityNotFoundException")]
    public async Task Should_Throw_ActivityNotFoundException()
    {
        // Arrange
        const string typeName = "MyMissingActivity";

        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName);
        await Assert.That(exception.MissingTypeName).IsEqualTo(typeName);
    }

    [Test]
    [DisplayName("NotFoundActivity includes type name in exception")]
    public async Task Should_Include_TypeName_In_Exception()
    {
        // Arrange
        const string typeName = "Custom.Namespace.MyActivity";

        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName);
        await Assert.That(exception.Message).Contains(typeName);
    }

    [Test]
    [DisplayName("NotFoundActivity includes version in exception")]
    public async Task Should_Include_Version_In_Exception()
    {
        // Arrange
        const string typeName = "MyMissingActivity";
        const int version = 2;

        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName, version);
        await Assert.That(exception.MissingTypeVersion).IsEqualTo(version);
        await Assert.That(exception.Message).Contains(version.ToString());
    }

    [Test]
    [DisplayName("NotFoundActivity preserves various type names")]
    [Arguments("SimpleActivity")]
    [Arguments("Namespace.Activity")]
    [Arguments("My.Custom.Namespace.ComplexActivity")]
    [Arguments("Activity123")]
    public async Task Should_Preserve_Various_TypeNames(string typeName)
    {
        // Act & Assert
        var exception = await ExecuteAndAssertExceptionAsync(typeName);
        await Assert.That(exception.MissingTypeName).IsEqualTo(typeName);
    }

    private static async Task<ActivityNotFoundException> ExecuteAndAssertExceptionAsync(string typeName, int version = 0)
    {
        var notFoundActivity = new NotFoundActivity(typeName, null, null)
        {
            MissingTypeVersion = version
        };
        return (await Assert.ThrowsExactlyAsync<ActivityNotFoundException>(() => ExecuteAsync(notFoundActivity)))!;
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }
}
