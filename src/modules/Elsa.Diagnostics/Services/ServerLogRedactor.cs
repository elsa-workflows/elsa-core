using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.Services;

public class ServerLogRedactor(IOptions<ServerLogStreamingOptions> options) : IServerLogRedactor
{
    private const string Redacted = "[Redacted]";
    private readonly HashSet<string> _sensitiveNames = options.Value.SensitiveNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    
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
        
        return IsSensitiveName(name) ? Redacted : value;
    }
    
    private bool IsSensitiveName(string name) => _sensitiveNames.Any(name.Contains);
}
