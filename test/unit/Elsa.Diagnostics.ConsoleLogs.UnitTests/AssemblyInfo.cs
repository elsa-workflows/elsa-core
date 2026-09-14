using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public static class AssemblyHooks
{
    [After(Assembly)]
    public static async Task RestoreConsoleAsync()
    {
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }
}
