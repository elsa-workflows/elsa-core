namespace Elsa.MassTransit.Services;

public static class AmbientConsumerScope
{
    private static readonly AsyncLocal<bool> IsRaisedFromConsumerState = new();

    public static bool IsRaisedFromConsumer
    {
        get => IsRaisedFromConsumerState.Value;
        set => IsRaisedFromConsumerState.Value = value;
    }
    
}