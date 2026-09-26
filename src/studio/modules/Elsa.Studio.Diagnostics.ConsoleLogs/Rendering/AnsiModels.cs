namespace Elsa.Studio.Diagnostics.Rendering;

/// <summary>
/// Standard 16-colour ANSI palette plus a sentinel for "default" (no explicit colour set).
/// </summary>
public enum AnsiColor
{
    Default,
    Black, Red, Green, Yellow, Blue, Magenta, Cyan, White,
    BrightBlack, BrightRed, BrightGreen, BrightYellow,
    BrightBlue, BrightMagenta, BrightCyan, BrightWhite
}

/// <summary>
/// Display attributes carried by every <see cref="AnsiSegment"/>.
/// Equality is by value so renderers can collapse adjacent segments with identical styling.
/// </summary>
public readonly record struct AnsiStyle(
    AnsiColor Foreground,
    AnsiColor Background,
    bool Bold,
    bool Italic,
    bool Underline,
    bool Inverse)
{
    public static AnsiStyle Default =>
        new(AnsiColor.Default, AnsiColor.Default, false, false, false, false);
}

/// <summary>
/// A run of text that shares a single <see cref="AnsiStyle"/>.
/// </summary>
public readonly record struct AnsiSegment(string Text, AnsiStyle Style);
