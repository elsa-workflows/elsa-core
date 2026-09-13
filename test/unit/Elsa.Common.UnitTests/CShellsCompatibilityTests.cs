using Elsa.Common.ShellFeatures;
using System.Threading.Tasks;

namespace Elsa.Common.UnitTests;

public class CShellsCompatibilityTests
{
    [Test]
    public async Task ElsaCommonTypesCanBeLoadedAgainstQuartzMinimumCShellsVersion()
    {
        await Assert.That(() => typeof(MultitenancyFeature).Assembly.GetTypes()).ThrowsNothing();
    }
}
