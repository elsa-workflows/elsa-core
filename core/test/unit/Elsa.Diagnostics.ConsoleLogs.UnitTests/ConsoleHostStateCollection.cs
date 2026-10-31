namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

/// <summary>
/// xUnit collection used by every test class that mutates the process-wide console stream hook
/// or console capture. Members of this collection run sequentially, preventing parallel tests from racing
/// on the same static state.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleHostStateCollection
{
    public const string Name = "ConsoleHostState";
}
