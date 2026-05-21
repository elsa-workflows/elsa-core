using Elsa.AI.Host.Streaming;

namespace Elsa.AI.IntegrationTests;

public class AiChatReconnectTests
{
    [Fact(DisplayName = "Disconnected chat can reconnect during grace window")]
    public void DisconnectedChatCanReconnectDuringGraceWindow()
    {
        var manager = new AiStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMinutes(5));

        Assert.True(manager.CanReconnect("conversation-1"));
    }
}
