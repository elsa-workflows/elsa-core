using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.Testing.Shared;

/// <summary>
/// General extension methods for ActivityTestFixture.
/// </summary>
public static class ActivityTestFixtureExtensions
{
    /// <summary>
    /// Validates that an activity has the expected attribute configuration.
    /// This is useful for ensuring activities are properly decorated with metadata.
    /// </summary>
    /// <param name="fixture">The test fixture (unused but enables extension method syntax)</param>
    /// <param name="expectedNamespace">Expected namespace (e.g., "Elsa")</param>
    /// /// <param name="expectedKind">Expected activity kind</param>
    /// <param name="expectedCategory">Expected category (e.g., "HTTP")</param>
    /// <param name="expectedDisplayName">Expected display name</param>
    /// <param name="expectedDescription">Expected description</param>
    public static void AssertActivityAttributes(
        this ActivityTestFixture fixture,
        string expectedNamespace,
        ActivityKind expectedKind,
        string? expectedCategory = null,
        string? expectedDisplayName = null,
        string? expectedDescription = null
        )
    {
        var activityType = fixture.Activity.GetType();
        var activityAttribute = activityType.GetCustomAttributes(typeof(ActivityAttribute), false)
            .Cast<ActivityAttribute>().FirstOrDefault();

        activityAttribute = TestAssert.NotNull(activityAttribute, $"Activity '{activityType.FullName}' has no {nameof(ActivityAttribute)}.");
        TestAssert.Equal(expectedNamespace, activityAttribute.Namespace, "The activity namespace does not match.");
        TestAssert.Equal(expectedKind, activityAttribute.Kind, "The activity kind does not match.");

        if (expectedCategory != null) TestAssert.Equal(expectedCategory, activityAttribute.Category, "The activity category does not match.");
        if (expectedDescription != null) TestAssert.Equal(expectedDescription, activityAttribute.Description, "The activity description does not match.");
        if (expectedDisplayName != null) TestAssert.Equal(expectedDisplayName, activityAttribute.DisplayName, "The activity display name does not match.");
    }
}
