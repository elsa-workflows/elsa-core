using System.Net;
using Elsa.Common;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Services;

public class LocalPasswordAttemptLimiterTests
{
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private DateTimeOffset _now = new(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);

    public LocalPasswordAttemptLimiterTests() => _clock.UtcNow.Returns(_ => _now);

    [Fact]
    public async Task DifferentValidatorScopesShareAnAccountBudgetUntilTheWindowExpires()
    {
        var limiter = CreateLimiter(accountLimit: 1, sourceLimit: 10);
        var users = Substitute.For<IUserProvider>();
        var hasher = Substitute.For<ISecretHasher>();
        var first = Validator("192.0.2.1");
        var second = Validator("192.0.2.2");

        Assert.Null(await first.ValidateAsync("Alice", "incorrect"));
        Assert.Null(await second.ValidateAsync("alice", "incorrect"));
        hasher.Received(1).HashSecret("incorrect");

        _now = _now.AddMinutes(1);
        var user = new User { Id = "user-1", Name = "alice", HashedPassword = "hash", HashedPasswordSalt = "salt" };
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>()).Returns(user);
        hasher.VerifySecret("correct", "hash", "salt", out _).Returns(true);
        Assert.Same(user, await second.ValidateAsync("alice", "correct"));
        Assert.Same(user, await second.ValidateAsync("alice", "correct"));

        DefaultUserCredentialsValidator Validator(string peer)
        {
            var context = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Parse(peer) } };
            var accessor = Substitute.For<IHttpContextAccessor>();
            accessor.HttpContext.Returns(context);
            return new(users, null, hasher, null, limiter, new TestTenantAccessor("tenant-a"), accessor);
        }
    }

    [Fact]
    public async Task SourceBudgetAppliesAcrossAccountsAndTenants()
    {
        var limiter = CreateLimiter(accountLimit: 2, sourceLimit: 1);
        await RecordFailureAsync(limiter, "tenant-a", "ALICE", "192.0.2.1");

        Assert.Null(await limiter.TryBeginAsync("tenant-b", "BOB", "192.0.2.1"));
        await using var independent = Assert.IsAssignableFrom<ILocalPasswordAttempt>(await limiter.TryBeginAsync("tenant-a", "BOB", "192.0.2.2"));
    }

    [Fact]
    public async Task ConcurrentReservationsAreBoundedAndExpiredReservationsStayExpired()
    {
        var limiter = CreateLimiter(accountLimit: 1, sourceLimit: 2, partitionLimit: 1);
        var attempts = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () => await limiter.TryBeginAsync("tenant-a", "ALICE", "192.0.2.1"))));
        var first = Assert.Single(attempts.OfType<ILocalPasswordAttempt>());
        Assert.Null(await limiter.TryBeginAsync("tenant-a", "BOB", "192.0.2.1"));

        _now = _now.AddMinutes(1);
        await RecordFailureAsync(limiter, "tenant-a", "ALICE", "192.0.2.1");
        await first.DisposeAsync();
        Assert.Null(await limiter.TryBeginAsync("tenant-a", "ALICE", "192.0.2.1"));
    }

    private ILocalPasswordAttemptLimiter CreateLimiter(int accountLimit, int sourceLimit, int partitionLimit = 4096) =>
        new MemoryLocalPasswordAttemptLimiter(_clock, Microsoft.Extensions.Options.Options.Create(new LocalPasswordAttemptOptions
        {
            AccountLimit = accountLimit,
            SourceLimit = sourceLimit,
            PartitionLimit = partitionLimit
        }));

    private static async Task RecordFailureAsync(ILocalPasswordAttemptLimiter limiter, string tenantId, string username, string source)
    {
        await using var attempt = Assert.IsAssignableFrom<ILocalPasswordAttempt>(await limiter.TryBeginAsync(tenantId, username, source));
        attempt.MarkFailure();
    }
}
