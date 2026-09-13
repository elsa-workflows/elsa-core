using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.HostedServices;

public class StoredPermissionValidatorTests
{
    private readonly CapturingLogger<StoredPermissionValidator> _logger = new();

    [Test]
    [Arguments("*")]
    [Arguments("workflows/definitions:view")]
    [Arguments("workflows/definitions:*")]
    [Arguments("workflows/*:view")]             // reaches 'workflows/definitions'
    [Arguments("workflows/definitions/*:view")] // reaches the prefix resource itself
    public async Task DoesNotWarnAboutAPermissionThatResolves(string permission)
    {
        await StartAsync(permission);

        await Assert.That(_logger.Entries).DoesNotContain(x => x.Level == LogLevel.Warning);
    }

    [Test]
    [Arguments("workflow/*:view")]     // typo'd subtree: reaches nothing, silently authorizes nothing
    [Arguments("secrets/*:view")]      // subtree over an unregistered branch
    [Arguments("workflows*:delete")]   // embedded wildcard: parses, but the matcher never satisfies it
    [Arguments("work*/foo/*:view")]
    [Arguments("workflows/definitions:frobnicate")]
    [Arguments("workflows/*:frobnicate")] // reaches 'workflows/definitions', which supports no such verb
    public async Task WarnsAboutAPermissionThatDoesNotResolve(string permission)
    {
        await StartAsync(permission);

        var warning = _logger.Entries.First(x => x.Level == LogLevel.Warning);
        await Assert.That(warning.Message).Contains("editors");
        await Assert.That(warning.Message).Contains(permission);
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