namespace Elsa.Identity.Contracts;

/// <summary>Reserves a local password attempt against the account and source budgets.</summary>
public interface ILocalPasswordAttemptLimiter
{
    /// <summary>
    /// Atomically reserves capacity across both budgets. Returns null when either budget is exhausted.
    /// The username is normalized and the source identifies the server-observed peer, when available.
    /// </summary>
    ValueTask<ILocalPasswordAttempt?> TryBeginAsync(string tenantId, string username, string? source, CancellationToken cancellationToken = default);
}

/// <summary>A reservation held while the server validates one password.</summary>
public interface ILocalPasswordAttempt : IAsyncDisposable
{
    /// <summary>Retains the reservation until the budget window expires. Disposal otherwise releases it.</summary>
    void MarkFailure();
}
