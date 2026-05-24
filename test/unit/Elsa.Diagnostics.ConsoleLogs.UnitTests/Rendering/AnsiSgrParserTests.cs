using Elsa.Diagnostics.ConsoleLogs.Rendering;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Rendering;

public class AnsiSgrParserTests
{
    [Fact]
    public void Parse_PlainTextHasNoStyling()
    {
        var segments = AnsiSgrParser.Parse("hello world");

        var segment = Assert.Single(segments);
        Assert.Equal("hello world", segment.Text);
        Assert.Equal(AnsiStyle.Default, segment.Style);
    }

    [Fact]
    public void Parse_BasicForegroundColor()
    {
        var segments = AnsiSgrParser.Parse("\u001b[31mred\u001b[0m tail");

        Assert.Collection(segments,
            s => Assert.Equal(("red", AnsiColor.Red), (s.Text, s.Style.Foreground)),
            s => Assert.Equal((" tail", AnsiColor.Default), (s.Text, s.Style.Foreground)));
    }

    [Fact]
    public void Parse_MicrosoftConsoleFormatterInfoLine()
    {
        // The exact SGR pattern Microsoft.Extensions.Logging.Console emits for an "info:" prefix
        // is: ESC[40m ESC[32m info ESC[39m ESC[22m ESC[49m: rest.
        var input = "\u001b[40m\u001b[32minfo\u001b[39m\u001b[22m\u001b[49m: rest";

        var segments = AnsiSgrParser.Parse(input);

        // The "info" run must carry green foreground and black background; everything after the reset
        // must be plain.
        var info = segments[0];
        Assert.Equal("info", info.Text);
        Assert.Equal(AnsiColor.Green, info.Style.Foreground);
        Assert.Equal(AnsiColor.Black, info.Style.Background);

        Assert.Equal(": rest", string.Concat(segments.Skip(1).Select(s => s.Text)));
        Assert.All(segments.Skip(1), s =>
        {
            Assert.Equal(AnsiColor.Default, s.Style.Foreground);
            Assert.Equal(AnsiColor.Default, s.Style.Background);
        });
    }

    [Fact]
    public void Parse_BoldAndUnderlineToggle()
    {
        var segments = AnsiSgrParser.Parse("\u001b[1;4mbold-underline\u001b[24mbold\u001b[22mplain");

        Assert.Collection(segments,
            s => { Assert.Equal("bold-underline", s.Text); Assert.True(s.Style.Bold); Assert.True(s.Style.Underline); },
            s => { Assert.Equal("bold", s.Text); Assert.True(s.Style.Bold); Assert.False(s.Style.Underline); },
            s => { Assert.Equal("plain", s.Text); Assert.False(s.Style.Bold); });
    }

    [Fact]
    public void Parse_BrightColorRange()
    {
        var segments = AnsiSgrParser.Parse("\u001b[92mbright-green");

        Assert.Equal(AnsiColor.BrightGreen, segments[0].Style.Foreground);
    }

    [Fact]
    public void Parse_ResetWithEmptyParameters()
    {
        var segments = AnsiSgrParser.Parse("\u001b[31mred\u001b[mreset");

        Assert.Equal(AnsiColor.Red, segments[0].Style.Foreground);
        Assert.Equal(AnsiColor.Default, segments[1].Style.Foreground);
    }

    [Fact]
    public void Parse_NonSgrCsiSequencesAreDropped()
    {
        // Cursor up + erase to end of line + colored content.
        var segments = AnsiSgrParser.Parse("\u001b[2A\u001b[K\u001b[31mred");

        Assert.Equal("red", segments[0].Text);
        Assert.Equal(AnsiColor.Red, segments[0].Style.Foreground);
    }

    [Fact]
    public void Parse_UnterminatedEscapeIsPreservedAsText()
    {
        // An ESC without a final byte must not silently swallow the rest of the line.
        var segments = AnsiSgrParser.Parse("good \u001b[31");

        var combined = string.Concat(segments.Select(s => s.Text));
        Assert.Contains("good ", combined);
        Assert.Contains("\u001b[31", combined);
    }

    [Fact]
    public void Parse_Color256IsConsumedButDegradesToDefault()
    {
        var segments = AnsiSgrParser.Parse("\u001b[38;5;208morange-ish\u001b[0m");

        var first = segments[0];
        Assert.Equal("orange-ish", first.Text);
        Assert.Equal(AnsiColor.Default, first.Style.Foreground);
    }

    [Fact]
    public void Parse_AdjacentSegmentsWithSameStyleAreCoalesced()
    {
        // The console formatter often emits a no-op SGR between identical styles; we collapse those.
        var segments = AnsiSgrParser.Parse("\u001b[31ma\u001b[31mb");

        var segment = Assert.Single(segments);
        Assert.Equal("ab", segment.Text);
    }
}

