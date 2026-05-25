using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

/// <summary>
/// xUnit collection used by every test class that mutates the process-wide <see cref="ConsoleStreamHook"/>
/// or <see cref="ConsoleLogsHost"/>. Members of this collection run sequentially, preventing parallel
/// tests from racing on the same static state.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ConsoleHostStateCollection
{
    public const string Name = "ConsoleHostState";
}

