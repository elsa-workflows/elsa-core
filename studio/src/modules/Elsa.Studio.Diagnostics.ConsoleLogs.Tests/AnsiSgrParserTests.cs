using Elsa.Studio.Diagnostics.Rendering;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class AnsiSgrParserTests
{
    [Fact]
    public void PlainTextProducesSingleDefaultSegment()
    {
        var s = Assert.Single(AnsiSgrParser.Parse("hello world"));
        Assert.Equal("hello world", s.Text);
        Assert.Equal(AnsiStyle.Default, s.Style);
    }

    [Fact]
    public void BasicForegroundColor()
    {
        var segments = AnsiSgrParser.Parse("\u001b[31mred\u001b[0m tail");
        Assert.Equal(AnsiColor.Red, segments[0].Style.Foreground);
        Assert.Equal(AnsiColor.Default, segments[1].Style.Foreground);
    }

    [Fact]
    public void MicrosoftConsoleFormatterInfoLine()
    {
        // ESC[40m ESC[32m info ESC[39m ESC[22m ESC[49m: rest
        var segments = AnsiSgrParser.Parse("\u001b[40m\u001b[32minfo\u001b[39m\u001b[22m\u001b[49m: rest");
        Assert.Equal(AnsiColor.Green, segments[0].Style.Foreground);
        Assert.Equal(AnsiColor.Black, segments[0].Style.Background);
        Assert.All(segments.Skip(1), s => Assert.Equal(AnsiColor.Default, s.Style.Foreground));
    }

    [Fact]
    public void NonSgrCsiSequencesAreDropped()
    {
        var segments = AnsiSgrParser.Parse("\u001b[2A\u001b[K\u001b[31mred");
        Assert.Equal("red", segments[0].Text);
        Assert.Equal(AnsiColor.Red, segments[0].Style.Foreground);
    }

    [Fact]
    public void UnterminatedEscapeIsPreservedAsText()
    {
        var combined = string.Concat(AnsiSgrParser.Parse("good \u001b[31").Select(s => s.Text));
        Assert.Contains("good ", combined);
        Assert.Contains("\u001b[31", combined);
    }

    [Fact]
    public void AdjacentSameStyleSegmentsAreCoalesced()
    {
        var s = Assert.Single(AnsiSgrParser.Parse("\u001b[31ma\u001b[31mb"));
        Assert.Equal("ab", s.Text);
    }
}
