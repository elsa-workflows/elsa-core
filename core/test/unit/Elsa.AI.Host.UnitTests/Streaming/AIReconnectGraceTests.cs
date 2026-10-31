using Elsa.AI.Host.Streaming;

namespace Elsa.AI.Host.UnitTests.Streaming;

public class AIReconnectGraceTests
{
    [Fact(DisplayName = "Stream session can reconnect during grace window")]
    public void StreamSessionCanReconnectDuringGraceWindow()
    {
        var manager = new AIStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMinutes(1));

        Assert.True(manager.CanReconnect("conversation-1"));
    }
}
