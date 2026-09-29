using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Maps diagnostics console URL query state.
/// </summary>
public class ConsoleLogUrlStateMapper
{
    /// <summary>
    /// Applies canonical query parameters to the given view state.
    /// </summary>
    public void ApplyQuery(ConsoleLogViewState state, Uri uri)
    {
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("source", out var source))
            state.Filter.SourceId = Normalize(source);

        if (query.TryGetValue("stream", out var stream))
            state.Filter.Stream = ParseStream(stream);

        if (query.TryGetValue("text", out var text))
            state.Filter.Query = Normalize(text);

        if (query.TryGetValue("from", out var from) && DateTimeOffset.TryParse(from, out var parsedFrom))
            state.Filter.From = parsedFrom;

        if (query.TryGetValue("to", out var to) && DateTimeOffset.TryParse(to, out var parsedTo))
            state.Filter.To = parsedTo;

        if (query.TryGetValue("wrap", out var wrap) && bool.TryParse(wrap, out var parsedWrap))
            state.Wrap = parsedWrap;

        if (query.TryGetValue("compact", out var compact) && bool.TryParse(compact, out var parsedCompact))
            state.Compact = parsedCompact;

        if (query.TryGetValue("ansi", out var ansi) && bool.TryParse(ansi, out var parsedAnsi))
            state.Ansi = parsedAnsi;

        if (query.TryGetValue("follow", out var follow) && bool.TryParse(follow, out var parsedFollow))
            state.FollowTail = parsedFollow;
    }

    /// <summary>
    /// Creates canonical query parameters for the given state.
    /// </summary>
    public Dictionary<string, object?> ToQueryParameters(ConsoleLogViewState state) =>
        new()
        {
            ["source"] = state.Filter.SourceId,
            ["stream"] = FormatStream(state.Filter.Stream),
            ["text"] = state.Filter.Query,
            ["from"] = state.Filter.From?.ToString("O"),
            ["to"] = state.Filter.To?.ToString("O"),
            ["wrap"] = FormatBool(state.Wrap),
            ["compact"] = FormatBool(state.Compact),
            ["ansi"] = FormatBool(state.Ansi),
            ["follow"] = FormatBool(state.FollowTail)
        };

    /// <summary>
    /// Parses stream selection values.
    /// </summary>
    public static ConsoleLogStream? ParseStream(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "stdout" => ConsoleLogStream.Stdout,
            "stderr" => ConsoleLogStream.Stderr,
            _ => null
        };
    }

    /// <summary>
    /// Formats the selected stream.
    /// </summary>
    public static string FormatStream(ConsoleLogStream? stream) => stream switch
    {
        ConsoleLogStream.Stdout => "stdout",
        ConsoleLogStream.Stderr => "stderr",
        _ => "both"
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatBool(bool value) => value ? "true" : "false";
}
