using Elsa.Diagnostics.ConsoleLogs.RealTime;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsHubTests
{
    [Fact]
    public void Hub_ExposesSubscriptionMethods()
    {
        Assert.NotNull(typeof(ElsaConsoleLogsHub).GetMethod(nameof(ElsaConsoleLogsHub.SubscribeAsync)));
        Assert.NotNull(typeof(ElsaConsoleLogsHub).GetMethod(nameof(ElsaConsoleLogsHub.UpdateFilterAsync)));
        Assert.NotNull(typeof(ElsaConsoleLogsHub).GetMethod(nameof(ElsaConsoleLogsHub.UnsubscribeAsync)));
    }
}
