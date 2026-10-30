using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class CssIsolationHostContractTests
{
    [Theory]
    [InlineData("ServerHost.cshtml", "Elsa.Studio.Host.Server.styles.css")]
    [InlineData("WasmHost.html", "Elsa.Studio.Host.Wasm.styles.css")]
    [InlineData("HostedWasmHost.cshtml", "Elsa.Studio.Host.Wasm.styles.css")]
    public void SupportedHosts_LoadTheirCssIsolationBundle(string hostAsset, string stylesheet)
    {
        var hostDocument = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "HostAssets", hostAsset));

        Assert.Contains($"href=\"{stylesheet}\"", hostDocument, StringComparison.Ordinal);
    }
}
