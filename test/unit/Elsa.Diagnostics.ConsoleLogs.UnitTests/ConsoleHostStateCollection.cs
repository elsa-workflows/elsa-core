namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

/// <summary>
/// TUnit constraint key used by tests that mutate the process-wide console stream hook or console capture.
/// </summary>
public sealed class ConsoleHostStateCollection
{
    public const string Name = "ConsoleHostState";
}
