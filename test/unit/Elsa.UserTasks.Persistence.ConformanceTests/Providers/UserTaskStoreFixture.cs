using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Providers;

/// <summary>
/// One provider's live stores, shared by every conformance class in that provider's collection.
///
/// Construction is deliberately lazy: xUnit builds a collection fixture even when every test in the
/// collection is skipped, so an unreachable provider must not try to connect here or a clean skip would
/// surface as an error.
/// </summary>
public abstract class UserTaskStoreFixture : IAsyncLifetime
{
    private readonly Lazy<Task> _activation;

    protected UserTaskStoreFixture(string providerName)
    {
        Provider = ConformanceProviders.Get(providerName);
        Options = Microsoft.Extensions.Options.Options.Create(Settings);
        _activation = new(ActivateCoreAsync);
    }

    public ConformanceProvider Provider { get; }
    public TestClock Clock { get; } = new();
    public UserTasksOptions Settings { get; } = new();
    public IOptions<UserTasksOptions> Options { get; }
    public IDataProtectionProvider DataProtection { get; } = new PassthroughDataProtectionProvider();

    /// <summary>Called by each test before it touches a store. Runs the real activation exactly once.</summary>
    public Task ActivateAsync()
    {
        if (!Provider.IsAvailable)
            throw new InvalidOperationException($"Provider '{Provider.Name}' is unavailable: {Provider.SkipReason}");
        return _activation.Value;
    }

    public abstract IUserTaskRepository Repository { get; }

    /// <summary>Overridden by providers that ship a guest session store. VNext deliberately does not.</summary>
    public virtual IUserTaskGuestSessionIssuer GuestSessions =>
        throw new NotSupportedException($"Provider '{Provider.Name}' has no guest session store.");

    /// <summary>Overridden by providers that ship an invitation outbox. VNext deliberately does not.</summary>
    public virtual IUserTaskInvitationOutbox Outbox =>
        throw new NotSupportedException($"Provider '{Provider.Name}' has no invitation outbox.");

    /// <summary>Creates a second repository over the same underlying store, for concurrent-writer tests.</summary>
    public abstract IUserTaskRepository CreateSecondRepository();

    protected abstract Task ActivateCoreAsync();

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeCoreAsync();

    protected virtual Task DisposeCoreAsync() => Task.CompletedTask;

    public sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

        public DateTimeOffset Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);
    }

    /// <summary>
    /// Data Protection stand-in. The outbox's contract is only that the ciphertext round-trips and that an
    /// unreadable payload is dropped, so the double marks the payload instead of pulling the full
    /// key-management stack into a unit test.
    /// </summary>
    private sealed class PassthroughDataProtectionProvider : IDataProtectionProvider, IDataProtector
    {
        private const string Marker = "protected:";

        public IDataProtector CreateProtector(string purpose) => this;

        public byte[] Protect(byte[] plaintext) => System.Text.Encoding.UTF8.GetBytes(Marker + Convert.ToBase64String(plaintext));

        public byte[] Unprotect(byte[] protectedData)
        {
            var value = System.Text.Encoding.UTF8.GetString(protectedData);
            if (!value.StartsWith(Marker, StringComparison.Ordinal))
                throw new System.Security.Cryptography.CryptographicException("The payload was not protected by this provider.");
            return Convert.FromBase64String(value[Marker.Length..]);
        }
    }
}
