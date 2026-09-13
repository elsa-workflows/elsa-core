using Elsa.AI.Host.Streaming;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Streaming;

public class AIReconnectGraceTests
{
    [Test]
    [DisplayName("Stream session can reconnect during grace window")]
    public async Task StreamSessionCanReconnectDuringGraceWindow()
    {
        var manager = new AIStreamSessionManager();

        manager.MarkDisconnected("conversation-1", TimeSpan.FromMinutes(1));

        await Assert.That(manager.CanReconnect("conversation-1")).IsTrue();
    }
}