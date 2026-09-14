namespace Elsa.Workflows.ComponentTests.Services;

/// <summary>
/// Mutable JavaScript value scoped to one component-test host.
/// </summary>
public sealed class TestJavaScriptState
{
    private object? _value;

    public object? Value
    {
        get => Volatile.Read(ref _value);
        set => Volatile.Write(ref _value, value);
    }
}
