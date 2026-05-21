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

    [Fact(DisplayName = "Reconnect deadline is removed after successful reconnect")]
    public void ReconnectDeadlineIsRemovedAfterSuccessfulReconnect()
    {
        var manager = new AiStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMinutes(5));

        Assert.True(manager.CanReconnect("conversation-1"));
        manager.MarkConnected("conversation-1");
        Assert.False(manager.CanReconnect("conversation-1"));
    }

    [Fact(DisplayName = "Expired reconnect deadline is removed")]
    public void ExpiredReconnectDeadlineIsRemoved()
    {
        var manager = new AiStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMilliseconds(-1));

        Assert.False(manager.CanReconnect("conversation-1"));
        Assert.False(manager.CanReconnect("conversation-1"));
    }
}
