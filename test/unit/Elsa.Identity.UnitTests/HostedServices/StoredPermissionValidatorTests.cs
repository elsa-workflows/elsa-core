using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Identity.UnitTests.HostedServices;

public class StoredPermissionValidatorTests
{
    private readonly CapturingLogger<StoredPermissionValidator> _logger = new();

    [Theory]
    [InlineData("*")]
    [InlineData("workflows/definitions:view")]
    [InlineData("workflows/definitions:*")]
    [InlineData("workflows/*:view")]             // reaches 'workflows/definitions'
    [InlineData("workflows/definitions/*:view")] // reaches the prefix resource itself
    public async Task DoesNotWarnAboutAPermissionThatResolves(string permission)
    {
        await StartAsync(permission);

        Assert.DoesNotContain(_logger.Entries, x => x.Level == LogLevel.Warning);
    }

    [Theory]
    [InlineData("workflow/*:view")]     // typo'd subtree: reaches nothing, silently authorizes nothing
    [InlineData("secrets/*:view")]      // subtree over an unregistered branch
    [InlineData("workflows*:delete")]   // embedded wildcard: parses, but the matcher never satisfies it
    [InlineData("work*/foo/*:view")]
    [InlineData("workflows/definitions:frobnicate")]
    [InlineData("workflows/*:frobnicate")] // reaches 'workflows/definitions', which supports no such verb
    public async Task WarnsAboutAPermissionThatDoesNotResolve(string permission)
    {
        await StartAsync(permission);

        var warning = _logger.Entries.First(x => x.Level == LogLevel.Warning);
        Assert.Contains("editors", warning.Message);
        Assert.Contains(permission, warning.Message);
    }

    private async Task StartAsync(string permission)
    {
        var role = new Role { Id = "role-1", Name = "editors", Permissions = [permission] };
        var registry = new DefaultPermissionDescriptorRegistry([new StubDescriptorProvider()]);

        var services = new ServiceCollection()
            .AddSingleton<IRoleProvider>(new StubRoleProvider(role))
            .AddSingleton<IPermissionDescriptorRegistry>(registry)
            .BuildServiceProvider();

        var validator = new StoredPermissionValidator(services.GetRequiredService<IServiceScopeFactory>(), _logger);
        await validator.StartAsync(CancellationToken.None);
    }

    private sealed class StubDescriptorProvider : IPermissionDescriptorProvider
    {
        public IEnumerable<PermissionDescriptor> GetDescriptors() =>
            [new("workflows/definitions", ["view"], "Workflow definitions", "Workflow definitions.", "Workflows")];
    }

    private sealed class StubRoleProvider(params Role[] roles) : IRoleProvider
    {
        public ValueTask<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<Role>>(roles);
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
