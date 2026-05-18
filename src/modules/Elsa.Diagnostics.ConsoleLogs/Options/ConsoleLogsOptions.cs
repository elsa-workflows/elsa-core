namespace Elsa.Diagnostics.ConsoleLogs.Options;

public class ConsoleLogsOptions
{
    public int RecentLogCapacity { get; set; } = 5_000;
    public int SubscriberChannelCapacity { get; set; } = 1_000;
    public int CaptureChannelCapacity { get; set; } = 5_000;
    public int MaxRecentQuerySize { get; set; } = 1_000;
    public int MaxLineLength { get; set; } = 16_384;
    public TimeSpan IdleFlushTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public bool StripAnsiEscapeSequences { get; set; } = true;
    public TimeSpan SourceHeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IncludeConsoleLogsInternalLogs { get; set; }
    public string RedactionReplacement { get; set; } = "[Redacted]";

    public ICollection<string> SensitiveNames { get; set; } =
    [
        "authorization",
        "token",
        "password",
        "secret",
        "api-key",
        "apikey",
        "cookie",
        "connection-string",
        "connectionstring"
    ];

    public ICollection<string> SensitiveTextPatterns { get; set; } =
    [
        "(?i)bearer\\s+[A-Za-z0-9._~+/=-]+",
        "(?i)(password|secret|token|api[-_]?key)\\s*[=:]\\s*[^\\s,;]+",
        "(?i)(AccountKey|SharedAccessKey)=([^;\\s]+)"
    ];
}
