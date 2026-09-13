using Elsa.AI.Host.Streaming;

namespace Elsa.AI.IntegrationTests;

public class AIChatReconnectTests
{
    [Test]
    [DisplayName("Disconnected chat can reconnect during grace window")]
    public async Task DisconnectedChatCanReconnectDuringGraceWindow()
    {
        var manager = new AIStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMinutes(5));

        await Assert.That(manager.CanReconnect("conversation-1")).IsTrue();
        manager.ReleaseReconnect("conversation-1");
        await Assert.That(manager.CanReconnect("conversation-1")).IsTrue();
    }

    [Test]
    [DisplayName("Reconnect eligibility is atomically reserved")]
    public async Task ReconnectEligibilityIsAtomicallyReserved()
    {
        var manager = new AIStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMinutes(5));

        await Assert.That(manager.CanReconnect("conversation-1")).IsTrue();
        await Assert.That(manager.CanReconnect("conversation-1")).IsFalse();
    }

    [Test]
    [DisplayName("Reconnect deadline is removed after successful reconnect")]
    public async Task ReconnectDeadlineIsRemovedAfterSuccessfulReconnect()
    {
        var manager = new AIStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMinutes(5));

        await Assert.That(manager.CanReconnect("conversation-1")).IsTrue();
        manager.MarkConnected("conversation-1");
        await Assert.That(manager.CanReconnect("conversation-1")).IsFalse();
    }

    [Test]
    [DisplayName("Expired reconnect deadline is removed")]
    public async Task ExpiredReconnectDeadlineIsRemoved()
    {
        var manager = new AIStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMilliseconds(-1));

        await Assert.That(manager.CanReconnect("conversation-1")).IsFalse();
        await Assert.That(manager.CanReconnect("conversation-1")).IsFalse();
    }
}
