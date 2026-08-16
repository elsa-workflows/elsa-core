using Elsa.Common.ShellFeatures;

namespace Elsa.Common.UnitTests;

public class CShellsCompatibilityTests
{
    [Fact]
    public void ElsaCommonTypesCanBeLoadedAgainstQuartzMinimumCShellsVersion()
    {
        var exception = Record.Exception(() => typeof(MultitenancyFeature).Assembly.GetTypes());

        Assert.Null(exception);
    }
}
