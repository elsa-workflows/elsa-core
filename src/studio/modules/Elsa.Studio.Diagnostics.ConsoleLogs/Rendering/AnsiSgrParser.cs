using System.Text;

namespace Elsa.Studio.Diagnostics.Rendering;

/// <summary>
/// Parses ANSI SGR (Select Graphic Rendition) escape sequences out of a text run and produces a
/// sequence of <see cref="AnsiSegment"/> records suitable for Blazor component rendering.
///
/// Non-SGR CSI and string-control sequences (cursor moves, screen clears, OSC titles) are stripped silently because
/// they are meaningless in a scrollback log. 256-colour and truecolour sequences are consumed
/// without error but map to <see cref="AnsiColor.Default"/>.
/// </summary>
public static class AnsiSgrParser
{
    private const char Escape = '\u001b';

    public static IReadOnlyList<AnsiSegment> Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var segments = new List<AnsiSegment>();
        var buffer = new StringBuilder(text.Length);
        var style = AnsiStyle.Default;
        var index = 0;

        while (index < text.Length)
        {
            var ch = text[index];

            if (ch != Escape)
            {
                buffer.Append(ch);
                index++;
                continue;
            }

            if (index + 1 >= text.Length)
                break;

            var introducer = text[index + 1];
            if (introducer != '[')
            {
                index = ConsumeEscapeSequence(text, index, introducer);
                continue;
            }

            // ESC '[': find the final byte (range @-~).
            var paramsStart = index + 2;
            var cursor = paramsStart;

            while (cursor < text.Length && !IsCsiFinalByte(text[cursor]))
                cursor++;

            if (cursor >= text.Length)
            {
                // Unterminated sequence: preserve as literal text.
                buffer.Append(text, index, text.Length - index);
                break;
            }

            var finalByte = text[cursor];
            var parameters = text.AsSpan(paramsStart, cursor - paramsStart);
            index = cursor + 1;

            // Only SGR ('m') changes style; all other CSI final bytes are dropped.
            if (finalByte != 'm')
                continue;

            Flush(segments, buffer, style);
            style = ApplySgr(parameters, style);
        }

        Flush(segments, buffer, style);
        return segments;
    }

    private static void Flush(List<AnsiSegment> segments, StringBuilder buffer, AnsiStyle style)
    {
        if (buffer.Length == 0)
            return;

        var text = buffer.ToString();
        buffer.Clear();

        // Coalesce adjacent segments with the same style.
        if (segments.Count > 0 && segments[^1].Style == style)
        {
            segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
            return;
        }

        segments.Add(new AnsiSegment(text, style));
    }

    private static AnsiStyle ApplySgr(ReadOnlySpan<char> parameters, AnsiStyle style)
    {
        if (parameters.IsEmpty)
            return AnsiStyle.Default;

        var codes = ParseCodes(parameters);

        for (var i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            switch (code)
            {
                case 0: style = AnsiStyle.Default; break;
                case 1: style = style with { Bold = true }; break;
                case 3: style = style with { Italic = true }; break;
                case 4: style = style with { Underline = true }; break;
                case 7: style = style with { Inverse = true }; break;
                case 22: style = style with { Bold = false }; break;
                case 23: style = style with { Italic = false }; break;
                case 24: style = style with { Underline = false }; break;
                case 27: style = style with { Inverse = false }; break;
                case >= 30 and <= 37: style = style with { Foreground = (AnsiColor)(AnsiColor.Black + (code - 30)) }; break;
                case 38: i = ConsumeExtended(codes, i, foreground: true, ref style); break;
                case 39: style = style with { Foreground = AnsiColor.Default }; break;
                case >= 40 and <= 47: style = style with { Background = (AnsiColor)(AnsiColor.Black + (code - 40)) }; break;
                case 48: i = ConsumeExtended(codes, i, foreground: false, ref style); break;
                case 49: style = style with { Background = AnsiColor.Default }; break;
                case >= 90 and <= 97: style = style with { Foreground = (AnsiColor)(AnsiColor.BrightBlack + (code - 90)) }; break;
                case >= 100 and <= 107: style = style with { Background = (AnsiColor)(AnsiColor.BrightBlack + (code - 100)) }; break;
            }
        }

        return style;
    }

    private static int ConsumeExtended(IReadOnlyList<int> codes, int index, bool foreground, ref AnsiStyle style)
    {
        if (index + 1 >= codes.Count)
            return index;

        var consumed = codes[index + 1] switch { 5 => 2, 2 => 4, _ => 1 };

        if (foreground) style = style with { Foreground = AnsiColor.Default };
        else style = style with { Background = AnsiColor.Default };

        return Math.Min(index + consumed, codes.Count - 1);
    }

    private static List<int> ParseCodes(ReadOnlySpan<char> parameters)
    {
        var codes = new List<int>();
        var current = 0;
        var any = false;

        foreach (var ch in parameters)
        {
            if (ch is ';' or ':') { codes.Add(any ? current : 0); current = 0; any = false; continue; }
            if (ch is >= '0' and <= '9') { current = (current * 10) + (ch - '0'); any = true; }
        }

        codes.Add(any ? current : 0);
        return codes;
    }

    private static bool IsCsiFinalByte(char ch) => ch is >= '@' and <= '~';

    private static int ConsumeEscapeSequence(string text, int escapeIndex, char introducer)
    {
        if (introducer == ']')
            return ConsumeOscSequence(text, escapeIndex + 2);

        if (introducer is 'P' or '^' or '_' or 'X')
            return ConsumeStringControlSequence(text, escapeIndex + 2);

        return escapeIndex + 2;
    }

    private static int ConsumeOscSequence(string text, int index)
    {
        while (index < text.Length)
        {
            if (text[index] == '\a')
                return index + 1;

            if (IsStringTerminator(text, index))
                return index + 2;

            index++;
        }

        return text.Length;
    }

    private static int ConsumeStringControlSequence(string text, int index)
    {
        while (index < text.Length)
        {
            if (IsStringTerminator(text, index))
                return index + 2;

            index++;
        }

        return text.Length;
    }

    private static bool IsStringTerminator(string text, int index) => text[index] == Escape && index + 1 < text.Length && text[index + 1] == '\\';
}
