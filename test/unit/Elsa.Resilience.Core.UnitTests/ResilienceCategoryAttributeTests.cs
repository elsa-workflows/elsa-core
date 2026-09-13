using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceCategoryAttributeTests
{
    [ResilienceCategory("HTTP")]
    private class CategorizedActivity;

    [Test]
    [DisplayName("Resilience category should be discoverable through reflection")]
    public async Task Category_IsReadableFromCustomAttributes()
    {
        var attribute = typeof(CategorizedActivity).GetCustomAttributes(typeof(ResilienceCategoryAttribute), false)
            .Cast<ResilienceCategoryAttribute>()
            .Single();

        await Assert.That(attribute.Category).IsEqualTo("HTTP");
    }

    [Test]
    [DisplayName("Resilience category should only be applicable to classes")]
    public async Task AttributeUsage_TargetsClassesOnly()
    {
        var usage = typeof(ResilienceCategoryAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Class);
    }
}
