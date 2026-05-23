using System.Reflection;
using System.Security.Claims;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Features;
using Elsa.Diagnostics.ConsoleLogs.Models;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsAuthorizationTests
{
    [Fact]
    public void HubAuthorization_RequiresAuthenticatedUser()
    {
        var authorize = Assert.Single(typeof(ConsoleLogsHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Null(authorize.Policy);
    }

    [Fact]
    public async Task HubSubscribe_WithoutConsoleLogsPermission_DeniesAccess()
    {
        var hub = CreateHub("write:diagnostics:console-logs");

        await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(new()));
    }

    [Fact]
    public async Task HubUpdateFilter_WithoutConsoleLogsPermission_DeniesAccess()
    {
        var hub = CreateHub("write:diagnostics:console-logs");

        await Assert.ThrowsAsync<HubException>(() => hub.UpdateFilterAsync(new()));
    }

    [Theory]
    [InlineData(ConsoleLogsPermissions.Read)]
    [InlineData(PermissionNames.All)]
    public async Task HubSubscribe_WithConsoleLogsPermission_AllowsAccess(string permission)
    {
        var hub = CreateHub(permission);

        await hub.SubscribeAsync(new());
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

            if (genericTypeDefinition == typeof(Elsa.Abstractions.ElsaEndpoint<,,>))
                return (genericArguments[0], genericArguments[1]);

            if (genericTypeDefinition == typeof(Elsa.Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), genericArguments[0]);
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
    }

    private static ConsoleLogsHub CreateHub(params string[] permissions)
    {
        var provider = new TestConsoleLogProvider();
        var sourceRegistry = new TestConsoleLogSourceRegistry();
        var hubContext = new TestHubContext();
        var subscriptionManager = new ConsoleLogSubscriptionManager(provider, sourceRegistry, hubContext, NullLogger<ConsoleLogSubscriptionManager>.Instance);

        return new ConsoleLogsHub(subscriptionManager)
        {
            Context = new TestHubCallerContext(CreateUser(permissions))
        };
    }

    private static ClaimsPrincipal CreateUser(params string[] permissions)
    {
        var permissionClaimType = (string)typeof(SecurityOptions)
            .GetProperty(nameof(SecurityOptions.PermissionsClaimType))!
            .GetValue(new Config().Security)!;
        var claims = permissions.Select(x => new Claim(permissionClaimType, x));
        var identity = new ClaimsIdentity(claims, "Test");

        return new ClaimsPrincipal(identity);
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

    private class TestConsoleLogSourceRegistry : IConsoleLogSourceRegistry
    {
        public event Action<ConsoleLogSource>? SourceChanged
        {
            add { }
            remove { }
        }

        public ConsoleLogSource Current { get; } = new()
        {
            Id = "test",
            DisplayName = "Test"
        };

        public void MarkSeen(string sourceId, DateTimeOffset timestamp)
        {
        }

        public IReadOnlyCollection<ConsoleLogSource> List()
        {
            return [Current];
        }
    }

    private class TestHubContext : IHubContext<ConsoleLogsHub, IConsoleLogsClient>
    {
        public IHubClients<IConsoleLogsClient> Clients { get; } = new TestHubClients();

        public IGroupManager Groups { get; } = new TestGroupManager();
    }

    private class TestHubClients : IHubClients<IConsoleLogsClient>
    {
        public IConsoleLogsClient All => throw new NotSupportedException();
        public IConsoleLogsClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IConsoleLogsClient Client(string connectionId) => throw new NotSupportedException();
        public IConsoleLogsClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IConsoleLogsClient Group(string groupName) => throw new NotSupportedException();
        public IConsoleLogsClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IConsoleLogsClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IConsoleLogsClient User(string userId) => throw new NotSupportedException();
        public IConsoleLogsClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private class TestGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class TestHubCallerContext(ClaimsPrincipal user) : HubCallerContext
    {
        public override string ConnectionId { get; } = "connection-1";
        public override string? UserIdentifier { get; } = "user-1";
        public override ClaimsPrincipal? User { get; } = user;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted { get; } = CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
