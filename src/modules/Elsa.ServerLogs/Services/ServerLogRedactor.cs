using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Options;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Elsa.ServerLogs.Services;

public class ServerLogRedactor(IOptions<ServerLogStreamingOptions> options) : IServerLogRedactor
{
    private const string Redacted = "[Redacted]";
    private readonly HashSet<string> _sensitiveNames = options.Value.SensitiveNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyCollection<Regex> _sensitiveTextPatterns = options.Value.SensitiveTextPatterns
        .Select(pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant))
        .ToList();
    
    public ServerLogEvent Redact(ServerLogEvent logEvent)
    {
        return logEvent with
        {
            Message = RedactValue("message", logEvent.Message) ?? string.Empty,
            MessageTemplate = RedactValue("messageTemplate", logEvent.MessageTemplate),
            Exception = RedactException(logEvent.Exception),
            Scopes = RedactDictionary(logEvent.Scopes),
            Properties = RedactDictionary(logEvent.Properties)
        };
    }
    
    private ServerLogException? RedactException(ServerLogException? exception)
    {
        if (exception == null)
            return null;
        
        return exception with
        {
            Message = RedactValue("exceptionMessage", exception.Message) ?? string.Empty,
            StackTrace = RedactValue("stackTrace", exception.StackTrace)
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
            return Redacted;

        return _sensitiveTextPatterns.Aggregate(value, (current, pattern) => pattern.Replace(current, Redacted));
    }
    
    private bool IsSensitiveName(string name) => _sensitiveNames.Any(sensitiveName => name.Contains(sensitiveName, StringComparison.OrdinalIgnoreCase));
}
