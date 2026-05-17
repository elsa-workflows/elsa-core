using Elsa.Diagnostics.ConsoleLogs.RealTime;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsHubTests
{
    [Fact]
    public void Hub_ExposesSubscriptionMethods()
    {
        Assert.NotNull(typeof(ConsoleLogsHub).GetMethod(nameof(ConsoleLogsHub.SubscribeAsync)));
        Assert.NotNull(typeof(ConsoleLogsHub).GetMethod(nameof(ConsoleLogsHub.UpdateFilterAsync)));
        Assert.NotNull(typeof(ConsoleLogsHub).GetMethod(nameof(ConsoleLogsHub.UnsubscribeAsync)));
    }
}
