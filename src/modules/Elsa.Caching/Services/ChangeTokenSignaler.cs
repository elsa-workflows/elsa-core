using System.Collections.Concurrent;
using Elsa.Caching.Contracts;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Services;

/// <inheritdoc />
public class ChangeTokenSignaler : IChangeTokenSignaler
{
    private readonly ConcurrentDictionary<string, ChangeTokenInfo> _changeTokens = new();

    /// <inheritdoc />
    public IChangeToken GetToken(string key)
    {
        return _changeTokens.GetOrAdd(
            key,
            _ =>
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var changeToken = new CancellationChangeToken(cancellationTokenSource.Token);
                return new ChangeTokenInfo(changeToken, cancellationTokenSource);
            }).ChangeToken;
    }

    /// <inheritdoc />
    public void TriggerToken(string key)
    {
        if (_changeTokens.TryRemove(key, out var changeTokenInfo))
            changeTokenInfo.TokenSource.Cancel();
    }

    private readonly struct ChangeTokenInfo(IChangeToken changeToken, CancellationTokenSource tokenSource)
    {
        public IChangeToken ChangeToken { get; } = changeToken;
        public CancellationTokenSource TokenSource { get; } = tokenSource;
    }
}