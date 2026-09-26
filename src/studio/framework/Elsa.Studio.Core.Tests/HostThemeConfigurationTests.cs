using Xunit;

namespace Elsa.Studio.Core.Tests;

public class HostThemeConfigurationTests
{
    [Theory]
    [InlineData("Elsa.Studio.Host.Server", "Program.cs")]
    [InlineData("Elsa.Studio.Host.Wasm", "Program.cs")]
    public void Hosts_bind_the_presentation_theme_section(params string[] hostPath)
    {
        var program = CssContractTestContext.ReadRepositoryFile(["src", "hosts", .. hostPath]);

        Assert.Contains(
            "AddCore(options => configuration.GetSection(StudioThemeOptions.SectionName).Bind(options))",
            program,
            StringComparison.Ordinal);
    }
}
