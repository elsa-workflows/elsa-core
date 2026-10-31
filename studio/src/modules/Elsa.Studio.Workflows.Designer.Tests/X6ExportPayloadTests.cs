using System.Text.Json;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

/// <summary>
/// Pins the payload handed to the <c>exportGraph</c> JavaScript function. That function switches on lowercase format
/// names, so an enum written as its member name ("Png") or as its numeric value would either be rejected outright or,
/// worse, fall through to a format the user did not ask for.
/// </summary>
public class X6ExportPayloadTests
{
    [Theory]
    [InlineData(ExportGraphFormat.Png, "png")]
    [InlineData(ExportGraphFormat.Jpeg, "jpeg")]
    [InlineData(ExportGraphFormat.Svg, "svg")]
    public void TheFormatIsWrittenAsTheLowercaseNameTheJavaScriptSwitchesOn(ExportGraphFormat format, string expected)
    {
        var payload = X6GraphApi.CreateExportPayload(new() { Format = format, FileName = "diagram" });

        Assert.Equal(JsonValueKind.String, payload.GetProperty("format").ValueKind);
        Assert.Equal(expected, payload.GetProperty("format").GetString());
    }

    [Fact]
    public void TheFileNameAndPaddingAreWrittenUnderTheNamesTheJavaScriptReads()
    {
        var payload = X6GraphApi.CreateExportPayload(new() { FileName = "diagram", Padding = 30 });

        Assert.Equal("diagram", payload.GetProperty("fileName").GetString());
        Assert.Equal(30, payload.GetProperty("padding").GetInt32());
    }
}
