using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Identity.UnitTests.HostedServices;

public class IdentityBootstrapDiagnosticTests
{
    [Fact]
    public async Task ReportsAnInstanceNobodyCanSignInTo()
    {
        var logger = await StartAsync();

        var error = Assert.Single(logger.Entries, x => x.Level == LogLevel.Error);
        Assert.Contains("UseDefaultAdmin", error.Message);
        Assert.Contains("UseAdminApiKey", error.Message);
    }

    [Fact]
    public async Task StaysQuietWhenAnAdministratorIsSeeded()
    {
        var logger = await StartAsync(adminUserName: "admin", adminPassword: "secret");

        Assert.DoesNotContain(logger.Entries, x => x.Level == LogLevel.Error);
    }

    [Fact]
    public async Task StaysQuietWhenAnAdminApiKeyIsConfigured()
    {
        var logger = await StartAsync(apiKey: "an-api-key");

        Assert.DoesNotContain(logger.Entries, x => x.Level == LogLevel.Error);
    }

    [Fact]
    public async Task StaysQuietWhenUsersAlreadyExist()
    {
        // The check is about an unusable instance, not about how it was bootstrapped: once anyone can sign in,
        // an operator who configured nothing declaratively is making a deliberate choice.
        var logger = await StartAsync(existingUser: new() { Id = "1", Name = "someone" });

        Assert.DoesNotContain(logger.Entries, x => x.Level == LogLevel.Error);
    }

    [Fact]
    public async Task SurvivesAStoreItCannotRead()
    {
        // The whole point of the broad catch: a store that cannot be read yet -- an unmigrated database, a
        // connection that is not up -- must not be the reason the host fails to start. That failure surfaces
        // on its own the moment a real request touches the store.
        var logger = await StartAsync(storeFailure: new InvalidDataException("the database is not migrated"));

        Assert.DoesNotContain(logger.Entries, x => x.Level == LogLevel.Error);
        Assert.Single(logger.Entries, x => x.Level == LogLevel.Debug);
    }

    private static async Task<CapturingLogger<IdentityBootstrapDiagnostic>> StartAsync(
        string adminUserName = "",
        string adminPassword = "",
        string apiKey = "",
        User? existingUser = null,
        Exception? storeFailure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUserStore>(new StubUserStore(existingUser, storeFailure));
        var logger = new CapturingLogger<IdentityBootstrapDiagnostic>();

        await using var serviceProvider = services.BuildServiceProvider();

        var diagnostic = new IdentityBootstrapDiagnostic(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            OptionsFactory.Create(new DefaultAdminUserOptions { AdminUserName = adminUserName, AdminPassword = adminPassword }),
            OptionsFactory.Create(new AdminApiKeyOptions { ApiKey = apiKey }),
            logger);

        await diagnostic.StartAsync(default);
        return logger;
    }

    private sealed class StubUserStore(User? user, Exception? failure = null) : IUserStore
    {
        public Task SaveAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default) =>
            failure is not null ? Task.FromException<IEnumerable<User>>(failure) : Task.FromResult<IEnumerable<User>>(user is null ? [] : [user]);

        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(user);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(new(logLevel, formatter(state, exception)));
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
