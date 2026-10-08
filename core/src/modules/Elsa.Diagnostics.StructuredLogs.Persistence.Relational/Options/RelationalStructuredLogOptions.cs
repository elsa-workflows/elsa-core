namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Options;

public class RelationalStructuredLogOptions
{
    public StructuredLogWriteQueueOptions WriteQueue { get; set; } = new();
    public StructuredLogRetentionOptions Retention { get; set; } = new();
}

public class StructuredLogWriteQueueOptions
{
    public int Capacity { get; set; } = 10_000;
    public int BatchSize { get; set; } = 100;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan ShutdownFlushTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

public class StructuredLogRetentionOptions
{
    public TimeSpan? MaxAge { get; set; }
    public int? MaxRows { get; set; }
    public bool CleanupOnStartup { get; set; }
}
