namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceCategoryAttributeTests
{
    [ResilienceCategory("HTTP")]
    private class CategorizedActivity;

    [Fact(DisplayName = "Resilience category should be discoverable through reflection")]
    public void Category_IsReadableFromCustomAttributes()
    {
        var attribute = typeof(CategorizedActivity).GetCustomAttributes(typeof(ResilienceCategoryAttribute), false)
            .Cast<ResilienceCategoryAttribute>()
            .Single();

        Assert.Equal("HTTP", attribute.Category);
    }

    [Fact(DisplayName = "Resilience category should only be applicable to classes")]
    public void AttributeUsage_TargetsClassesOnly()
    {
        var usage = typeof(ResilienceCategoryAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
    }
}
