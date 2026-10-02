using Elsa.Studio.Workflows.Services;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the <c>.bpmn</c> vs <c>.json</c> routing decision the shared import path makes: a <c>.bpmn</c> file
/// dropped on the generic (JSON/ZIP) picker must be recognized so it can be routed through the interactive BPMN
/// import flow instead of being silently skipped by the JSON parser.
/// </summary>
public class BpmnFileRoutingTests
{
    [Theory]
    [InlineData("process.bpmn", true)]
    [InlineData("process.BPMN", true)]
    [InlineData("Process.Bpmn", true)]
    [InlineData("process.json", false)]
    [InlineData("process.zip", false)]
    [InlineData("process.bpmn.json", false)]
    [InlineData("bpmn", false)]
    public void IsBpmnFileName_DetectsTheBpmnExtensionCaseInsensitively(string fileName, bool expected)
    {
        Assert.Equal(expected, BpmnImportUiService.IsBpmnFileName(fileName));
    }

    [Fact]
    public void IsBpmnFile_DelegatesToTheFileName()
    {
        var service = new BpmnImportUiService(null!, null!, null!, null!);
        var file = new FakeBrowserFile("order-process.bpmn");

        Assert.True(service.IsBpmnFile(file));
    }

    [Fact]
    public void IsBpmnFile_ReturnsFalseForNonBpmnFiles()
    {
        var service = new BpmnImportUiService(null!, null!, null!, null!);
        var file = new FakeBrowserFile("order-process.json");

        Assert.False(service.IsBpmnFile(file));
    }

    private sealed class FakeBrowserFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => 0;
        public string ContentType => "application/xml";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream();
    }
}
