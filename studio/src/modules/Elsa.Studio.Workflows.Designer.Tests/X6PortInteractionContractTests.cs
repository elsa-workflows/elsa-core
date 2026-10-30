using System.Text.RegularExpressions;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public sealed class X6PortInteractionContractTests
{
    [Fact]
    public void ActivityPorts_UseLargerMagneticHitTargetsWithoutAVisibleDock()
    {
        var portRegistration = ReadAsset("designer-ports.ts");
        var graphCreation = ReadAsset("create-graph.ts");
        var designerStyles = ReadAsset("designer.v2.css");

        // A transparent circle makes the magnetic target substantially easier to acquire without
        // changing the normal, compact port appearance.
        Assert.Contains("selector: 'hitArea'", portRegistration, StringComparison.Ordinal);
        Assert.Matches(
            new Regex("hitArea:\\s*\\{[^}]*r:\\s*(?:1[2-9]|[2-9]\\d)", RegexOptions.CultureInvariant),
            portRegistration);
        Assert.Contains("selector: 'port'", portRegistration, StringComparison.Ordinal);
        Assert.Matches(
            new Regex("port:\\s*\\{[^}]*magnet:\\s*true", RegexOptions.CultureInvariant),
            portRegistration);
        Assert.DoesNotContain("selector: 'dock'", portRegistration, StringComparison.Ordinal);
        Assert.DoesNotContain(".elsa-designer-port-dock", designerStyles, StringComparison.Ordinal);
        Assert.Contains("selector: 'circle'", portRegistration, StringComparison.Ordinal);
        Assert.Matches(
            new Regex("circle:\\s*\\{[^}]*r:\\s*[5-9]", RegexOptions.CultureInvariant),
            portRegistration);

        // Native hover styling keeps the visual feedback transient and avoids persisting a
        // graph-model mutation or introducing event-handler lifecycle bookkeeping.
        Assert.Contains(".x6-port:hover .elsa-designer-port-circle", designerStyles, StringComparison.Ordinal);
        Assert.Matches(
            new Regex("\\.x6-port:hover \\.elsa-designer-port-circle\\s*\\{[^}]*r:\\s*(?:[6-9]|1\\d)px", RegexOptions.CultureInvariant),
            designerStyles);

        // A source port must remain connectable and accept another outgoing edge after one has
        // already been created from it.
        Assert.Contains("magnetConnectable: () => !readOnly && modePolicy.allowsConnections", graphCreation, StringComparison.Ordinal);
        Assert.Contains("allowMulti: true", graphCreation, StringComparison.Ordinal);
    }

    [Fact]
    public void ActivityNodes_UseDockedPortsWithoutAHandleLikeAccentBar()
    {
        var designerStyles = ReadAsset("designer.v2.css");

        Assert.DoesNotContain(".elsa-activity::before", designerStyles, StringComparison.Ordinal);
        Assert.Contains(".elsa-activity.is-starting-point", designerStyles, StringComparison.Ordinal);
        Assert.Contains("border-color: var(--elsa-activity-accent", designerStyles, StringComparison.Ordinal);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));
}
