using Elsa.Diagnostics.ConsoleLogs.RealTime;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsHubTests
{
    [Test]
    public async Task Hub_ExposesSubscriptionMethods()
    {
        await Assert.That(typeof(ElsaConsoleLogsHub).GetMethod(nameof(ElsaConsoleLogsHub.SubscribeAsync))).IsNotNull();
        await Assert.That(typeof(ElsaConsoleLogsHub).GetMethod(nameof(ElsaConsoleLogsHub.UpdateFilterAsync))).IsNotNull();
        await Assert.That(typeof(ElsaConsoleLogsHub).GetMethod(nameof(ElsaConsoleLogsHub.UnsubscribeAsync))).IsNotNull();
    }
}
