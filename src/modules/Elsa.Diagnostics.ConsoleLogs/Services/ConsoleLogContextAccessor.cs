using ConsoleLogStreaming.Core;
using Elsa.Diagnostics.ConsoleLogs.Contracts;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Ambient console log context backed by the current async execution context.
/// </summary>
public sealed class ConsoleLogContextAccessor : IConsoleLogContextAccessor, IConsoleLogMetadataAccessor
{
    private static readonly AsyncLocal<MetadataFrame?> CurrentFrame = new();
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    /// <summary>
    /// Gets the process-wide Elsa console log context accessor.
    /// </summary>
    public static ConsoleLogContextAccessor Instance { get; } = new();

    private ConsoleLogContextAccessor()
    {
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetMetadata()
    {
        var metadata = CurrentFrame.Value?.Metadata;
        return metadata == null || metadata.Count == 0
            ? Empty
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IDisposable PushMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A metadata key is required.", nameof(key));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A metadata value is required.", nameof(value));

        var previous = CurrentFrame.Value;
        var metadata = previous?.Metadata != null
            ? new Dictionary<string, string>(previous.Metadata, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        metadata[key] = value;
        CurrentFrame.Value = new(metadata);
        return new MetadataScope(previous);
    }

    /// <inheritdoc />
    public IDisposable PushWorkflowInstanceId(string workflowInstanceId) =>
        PushMetadata(ConsoleLogMetadataKeys.WorkflowInstanceId, workflowInstanceId);

    private sealed record MetadataFrame(IReadOnlyDictionary<string, string> Metadata);

    private sealed class MetadataScope(MetadataFrame? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            CurrentFrame.Value = previous;
            _disposed = true;
        }
    }
}
