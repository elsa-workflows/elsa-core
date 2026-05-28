using System.Text.RegularExpressions;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.Services;

public class OpenTelemetryRedactor(IOptions<OpenTelemetryDiagnosticsOptions> options) : IOpenTelemetryRedactor
{
    private const string Redacted = "[Redacted]";
    private readonly HashSet<string> _sensitiveNames = options.Value.SensitiveNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyCollection<Regex> _sensitiveTextPatterns = options.Value.SensitiveTextPatterns
        .Select(pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, options.Value.SensitiveTextPatternTimeout))
        .ToList();

    public OpenTelemetryBatch Redact(OpenTelemetryBatch batch)
    {
        return batch with
        {
            Resources = batch.Resources.Select(RedactResource).ToList(),
            Spans = batch.Spans.Select(RedactSpan).ToList(),
            Instruments = batch.Instruments.Select(x => x with { Attributes = RedactDictionary(x.Attributes) }).ToList(),
            MetricPoints = batch.MetricPoints.Select(x => x with { Attributes = RedactDictionary(x.Attributes) }).ToList(),
            Logs = batch.Logs.Select(RedactLog).ToList()
        };
    }

    private TelemetryResource RedactResource(TelemetryResource resource) => resource with { Attributes = RedactDictionary(resource.Attributes) };

    private TelemetrySpan RedactSpan(TelemetrySpan span)
    {
        return span with
        {
            Name = RedactValue("span.name", span.Name) ?? string.Empty,
            StatusDescription = RedactValue("span.status.description", span.StatusDescription),
            Attributes = RedactDictionary(span.Attributes),
            Events = span.Events.Select(x => x with { Attributes = RedactDictionary(x.Attributes) }).ToList(),
            Links = span.Links.Select(x => x with { Attributes = RedactDictionary(x.Attributes) }).ToList()
        };
    }

    private OtlpLogRecord RedactLog(OtlpLogRecord log)
    {
        return log with
        {
            Body = RedactValue("log.body", log.Body) ?? string.Empty,
            Attributes = RedactDictionary(log.Attributes)
        };
    }

    private Dictionary<string, string?> RedactDictionary(IDictionary<string, string?> values)
    {
        return values.ToDictionary(x => x.Key, x => RedactValue(x.Key, x.Value), StringComparer.OrdinalIgnoreCase);
    }

    private string? RedactValue(string name, string? value)
    {
        if (value == null)
            return null;

        if (_sensitiveNames.Any(sensitiveName => name.Contains(sensitiveName, StringComparison.OrdinalIgnoreCase)))
            return Redacted;

        return _sensitiveTextPatterns.Aggregate(value, RedactPattern);
    }

    private static string RedactPattern(string value, Regex pattern)
    {
        try
        {
            return pattern.Replace(value, Redacted);
        }
        catch (RegexMatchTimeoutException)
        {
            return value;
        }
    }
}
