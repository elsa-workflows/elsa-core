using Elsa.Diagnostics.ConsoleLogs.RealTime;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsHubSourceStatusTests
{
    [Fact]
    public void ClientContract_ExposesSourceChangeMethod()
    {
        Assert.NotNull(typeof(IConsoleLogsClient).GetMethod(nameof(IConsoleLogsClient.ReceiveSourceChangedAsync)));
    }
}
