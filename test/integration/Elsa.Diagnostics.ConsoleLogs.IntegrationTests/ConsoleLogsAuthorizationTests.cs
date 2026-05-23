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
        var (requestDtoType, responseDtoType) = GetEndpointDtoTypes(endpointType);
        var definition = new EndpointDefinition(endpointType, requestDtoType, responseDtoType);

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

    private static (Type RequestDtoType, Type ResponseDtoType) GetEndpointDtoTypes(Type endpointType)
    {
        var type = endpointType;

        while (type.BaseType != null)
        {
            type = type.BaseType;

            if (!type.IsGenericType)
                continue;

            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            if (genericTypeDefinition == typeof(Elsa.Abstractions.ElsaEndpoint<,>))
                return (genericArguments[0], genericArguments[1]);

            if (genericTypeDefinition == typeof(Elsa.Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), genericArguments[0]);
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
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
