using Microsoft.JSInterop;

namespace Elsa.Studio.Bpmn.Tests.Support;

/// <summary>An <see cref="IJSRuntime"/> whose only module is <see cref="HostModule"/>, so a test controls every call the host makes.</summary>
internal sealed class FakeJsRuntime : IJSRuntime
{
    public FakeJsObject HostModule { get; } = new();
    public List<(string Identifier, object?[] Args)> Calls { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add((identifier, args ?? []));
        return identifier == "import" ? ValueTask.FromResult((TValue)(object)HostModule) : throw new InvalidOperationException($"Unexpected call to '{identifier}'.");
    }
}

/// <summary>An <see cref="IJSObjectReference"/> that answers each function with the handler a test set for it.</summary>
internal sealed class FakeJsObject : IJSObjectReference
{
    public Dictionary<string, Func<object?[], Task<object?>>> Handlers { get; } = new();
    public List<(string Identifier, object?[] Args)> Calls { get; } = [];
    public bool IsDisposed { get; private set; }

    public void Returns(string identifier, object? value) => Handlers[identifier] = _ => Task.FromResult(value);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add((identifier, args ?? []));

        if (!Handlers.TryGetValue(identifier, out var handler))
            return default!;

        return (TValue)(await handler(args ?? []))!;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
