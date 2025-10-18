using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.Activities.UnitTests.Helpers;

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
    /// <param name="activityType">The type of activity to validate</param>
    /// <param name="expectedNamespace">Expected namespace (e.g., "Elsa")</param>
    /// <param name="expectedKind">Expected activity kind</param>
    /// <param name="expectedCategory">Expected category (e.g., "HTTP")</param>
    /// <param name="expectedDisplayName">Expected display name</param>
    /// <param name="expectedDescription">Expected description</param>
    public static void AssertActivityAttributes(
        this ActivityTestFixture fixture,
        Type activityType,
        string expectedNamespace,
        ActivityKind expectedKind,
        string? expectedCategory = null,
        string? expectedDisplayName = null,
        string? expectedDescription = null)
    {
        var activityAttribute = activityType.GetCustomAttributes(typeof(ActivityAttribute), false)
            .Cast<ActivityAttribute>().FirstOrDefault();

        Assert.NotNull(activityAttribute);
        Assert.Equal(expectedNamespace, activityAttribute.Namespace);
        Assert.Equal(expectedKind, activityAttribute.Kind);

        if (expectedCategory != null) Assert.Equal(expectedCategory, activityAttribute.Category);
        if (expectedDescription != null) Assert.Equal(expectedDescription, activityAttribute.Description);
        if (expectedDisplayName != null) Assert.Equal(expectedDisplayName, activityAttribute.DisplayName);
    }
}
