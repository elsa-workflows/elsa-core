namespace Elsa.MassTransit.Services;

/// Holds ambient state about a consumer.
public static class AmbientConsumerScope
{
    private static readonly AsyncLocal<bool> IsRaisedFromConsumerState = new();
    
    /// Gets or sets a value that indicates if the current code is initiated from a consumer.
    public static bool IsConsumerExecutionContext
    {
        get => IsRaisedFromConsumerState.Value;
        set => IsRaisedFromConsumerState.Value = value;
    }
}