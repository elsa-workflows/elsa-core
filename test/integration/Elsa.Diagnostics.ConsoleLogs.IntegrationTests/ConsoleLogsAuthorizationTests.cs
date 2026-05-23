using System.Reflection;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Features;
using Elsa.Diagnostics.ConsoleLogs.Models;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsAuthorizationTests
{
    [Fact]
    public void HubAuthorization_RequiresAuthenticatedUser()
    {
        var authorize = Assert.Single(typeof(ConsoleLogsHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Null(authorize.Policy);
    }

    [Theory]
    [InlineData("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint")]
    [InlineData("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Sources.Endpoint")]
    public void RestEndpoints_RequireConsoleLogsPermission(string endpointTypeName)
    {
        var permissions = GetConfiguredPermissions(endpointTypeName);

        Assert.Contains(ConsoleLogsPermissions.Read, permissions);
    }

    private static IReadOnlyCollection<string> GetConfiguredPermissions(string endpointTypeName)
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType(endpointTypeName, throwOnError: true)!;
        var endpoint = Activator.CreateInstance(endpointType, new TestConsoleLogProvider())!;
        var definition = new EndpointDefinition(endpointType, requestDtoType: null!, responseDtoType: null!);

        endpointType
            .GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, definition);

        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        var permissions = definition
            .GetType()
            .GetProperty("AllowedPermissions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(definition);

        return Assert.IsAssignableFrom<IEnumerable<string>>(permissions).ToArray();
    }

    private class TestConsoleLogProvider : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecentConsoleLogsResult([], []));
        }

        public IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<ConsoleLogStreamItem>();
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
        }
    }
}
