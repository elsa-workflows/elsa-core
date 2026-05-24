using System.Text;

namespace Elsa.Diagnostics.ConsoleLogs.Rendering;

/// <summary>
/// Parses ANSI SGR (Select Graphic Rendition) escape sequences out of a text run and produces a sequence
/// of <see cref="AnsiSegment"/> records suitable for client-side rendering (HTML spans, Blazor components,
/// MAUI runs, etc.).
///
/// <para>
/// Supports the standard 8/16-colour palette plus bold, italic, underline, and inverse. Non-SGR CSI
/// sequences (cursor moves, screen clears, OSC titles) are stripped silently because they are not
/// meaningful in a scrollback log. 256-colour and truecolour (38;5;N / 38;2;R;G;B) sequences are accepted
/// without error but mapped to <see cref="AnsiColor.Default"/> — extend <see cref="ApplyExtendedColor"/>
/// if you want richer fidelity.
/// </para>
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

            if (ch != Escape || index + 1 >= text.Length || text[index + 1] != '[')
            {
                buffer.Append(ch);
                index++;
                continue;
            }

            // We have an ESC '['. Find the final byte (the character in the range @-~).
            var paramsStart = index + 2;
            var cursor = paramsStart;

            while (cursor < text.Length && !IsCsiFinalByte(text[cursor]))
                cursor++;

            if (cursor >= text.Length)
            {
                // Unterminated sequence — keep what we had as literal text and stop scanning escapes here.
                buffer.Append(text, index, text.Length - index);
                break;
            }

            var finalByte = text[cursor];
            var parameters = text.AsSpan(paramsStart, cursor - paramsStart);
            index = cursor + 1;

            // SGR sequences end in 'm'. Any other final byte (H, J, K, …) is dropped without changing style.
            if (finalByte != 'm')
                continue;

            FlushSegment(segments, buffer, style);
            style = ApplySgrParameters(parameters, style);
        }

        FlushSegment(segments, buffer, style);
        return segments;
    }

    private static void FlushSegment(List<AnsiSegment> segments, StringBuilder buffer, AnsiStyle style)
    {
        if (buffer.Length == 0)
            return;

        // Coalesce with the previous segment when styles match — avoids fragmenting the output across
        // empty SGR resets and back-to-back identical sequences that some loggers emit.
        var text = buffer.ToString();
        buffer.Clear();

        if (segments.Count > 0 && segments[^1].Style == style)
        {
            var previous = segments[^1];
            segments[^1] = previous with { Text = previous.Text + text };
            return;
        }

        segments.Add(new AnsiSegment(text, style));
    }

    private static AnsiStyle ApplySgrParameters(ReadOnlySpan<char> parameters, AnsiStyle style)
    {
        // Empty parameters (ESC '[' 'm') is treated as reset, per the standard.
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
                case 38: i = ApplyExtendedColor(codes, i, foreground: true, ref style); break;
                case 39: style = style with { Foreground = AnsiColor.Default }; break;
                case >= 40 and <= 47: style = style with { Background = (AnsiColor)(AnsiColor.Black + (code - 40)) }; break;
                case 48: i = ApplyExtendedColor(codes, i, foreground: false, ref style); break;
                case 49: style = style with { Background = AnsiColor.Default }; break;
                case >= 90 and <= 97: style = style with { Foreground = (AnsiColor)(AnsiColor.BrightBlack + (code - 90)) }; break;
                case >= 100 and <= 107: style = style with { Background = (AnsiColor)(AnsiColor.BrightBlack + (code - 100)) }; break;
                    // All other codes are ignored deliberately.
            }
        }

        return style;
    }

    private static int ApplyExtendedColor(IReadOnlyList<int> codes, int index, bool foreground, ref AnsiStyle style)
    {
        // 38;5;N or 48;5;N (256-colour palette), 38;2;R;G;B or 48;2;R;G;B (truecolour).
        // The semantic palette doesn't model these, so we consume the parameters and leave the colour at default.
        if (index + 1 >= codes.Count)
            return index;

        var mode = codes[index + 1];
        var consumed = mode switch
        {
            5 => 2, // skip the palette index
            2 => 4, // skip R, G, B
            _ => 1
        };

        if (foreground)
            style = style with { Foreground = AnsiColor.Default };
        else
            style = style with { Background = AnsiColor.Default };

        return Math.Min(index + consumed, codes.Count - 1);
    }

    private static List<int> ParseCodes(ReadOnlySpan<char> parameters)
    {
        var codes = new List<int>();
        var current = 0;
        var any = false;

        foreach (var ch in parameters)
        {
            if (ch == ';' || ch == ':')
            {
                codes.Add(any ? current : 0);
                current = 0;
                any = false;
                continue;
            }

            if (ch is >= '0' and <= '9')
            {
                current = (current * 10) + (ch - '0');
                any = true;
            }
            // Anything else (private parameters, intermediate bytes) is ignored.
        }

        codes.Add(any ? current : 0);
        return codes;
    }

    private static bool IsCsiFinalByte(char ch) => ch is >= '@' and <= '~';
}

