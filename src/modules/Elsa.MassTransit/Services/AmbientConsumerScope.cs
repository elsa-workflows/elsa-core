namespace Elsa.MassTransit.Services;

/// <summary>
/// Holds ambient state about a consumer.
/// </summary>
public static class AmbientConsumerScope
{
    private static readonly AsyncLocal<bool> IsRaisedFromConsumerState = new();
    
    /// <summary>
    /// Gets or sets a value that indicates if the current code is initiated from a consumer.
    /// </summary>
    public static bool IsWorkflowDefinitionEventsConsumer
    {
        get => IsRaisedFromConsumerState.Value;
        set => IsRaisedFromConsumerState.Value = value;
    }
}