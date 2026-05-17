using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLogRedactor(IOptions<ConsoleLogsOptions> options) : IConsoleLogRedactor
{
    private readonly ConsoleLogsOptions _options = options.Value;
    private readonly HashSet<string> _sensitiveNames = options.Value.SensitiveNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyCollection<Regex> _sensitiveTextPatterns = options.Value.SensitiveTextPatterns
        .Select(pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant))
        .ToList();

    public ConsoleLogLine Redact(ConsoleLogLine line)
    {
        return line with
        {
            Text = RedactValue("text", line.Text) ?? string.Empty,
            Source = Redact(line.Source)
        };
    }

    public ConsoleLogSource Redact(ConsoleLogSource source)
    {
        return source with
        {
            Id = RedactValue("id", source.Id) ?? source.Id,
            DisplayName = RedactValue("displayName", source.DisplayName) ?? source.DisplayName,
            ServiceName = RedactValue("serviceName", source.ServiceName),
            MachineName = RedactValue("machineName", source.MachineName) ?? source.MachineName,
            PodName = RedactValue("podName", source.PodName),
            ContainerName = RedactValue("containerName", source.ContainerName),
            Namespace = RedactValue("namespace", source.Namespace),
            NodeName = RedactValue("nodeName", source.NodeName),
            Metadata = RedactDictionary(source.Metadata)
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

        if (IsSensitiveName(name))
            return _options.RedactionReplacement;

        return _sensitiveTextPatterns.Aggregate(value, (current, pattern) => pattern.Replace(current, _options.RedactionReplacement));
    }

    private bool IsSensitiveName(string name) => _sensitiveNames.Any(sensitiveName => name.Contains(sensitiveName, StringComparison.OrdinalIgnoreCase));
}
