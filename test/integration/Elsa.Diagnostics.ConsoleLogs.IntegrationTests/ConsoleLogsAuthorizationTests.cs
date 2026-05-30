using System.Reflection;
using System.Security.Claims;
using System.Text;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Features;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsAuthorizationTests
{
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
    [InlineData("read:*")]
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

    [Fact]
    public async Task RecentEndpoint_MapsWorkflowInstanceIdToMetadataFilter()
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint", throwOnError: true)!;
        var provider = new TestConsoleLogProvider();
        var endpoint = Activator.CreateInstance(endpointType, provider)!;
        SetJsonRequest(endpointType, endpoint,
            """
            {
                "workflowInstanceId": "workflow-instance-a"
            }
            """);

        var result = endpointType.GetMethod("ExecuteAsync", [typeof(CancellationToken)])!.Invoke(endpoint, [CancellationToken.None]);
        await Assert.IsAssignableFrom<Task>(result);

        Assert.NotNull(provider.LastFilter);
        var metadata = provider.LastFilter.Metadata;
        Assert.True(metadata.TryGetValue(ConsoleLogMetadataKeys.WorkflowInstanceId, out var workflowInstanceId));
        Assert.Equal("workflow-instance-a", workflowInstanceId);
    }

    [Theory]
    [InlineData("stdout", ConsoleStream.Stdout)]
    [InlineData("stderr", ConsoleStream.Stderr)]
    public async Task RecentEndpoint_MapsLowercaseStreamFilter(string stream, ConsoleStream expected)
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint", throwOnError: true)!;
        var provider = new TestConsoleLogProvider();
        var endpoint = Activator.CreateInstance(endpointType, provider)!;
        SetJsonRequest(endpointType, endpoint,
            $$"""
            {
                "stream": "{{stream}}"
            }
            """);

        var result = endpointType.GetMethod("ExecuteAsync", [typeof(CancellationToken)])!.Invoke(endpoint, [CancellationToken.None]);
        await Assert.IsAssignableFrom<Task>(result);

        Assert.NotNull(provider.LastFilter);
        Assert.Equal(expected, provider.LastFilter.Stream);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("all")]
    public async Task RecentEndpoint_MapsAllStreamFilterToNull(string? stream)
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint", throwOnError: true)!;
        var provider = new TestConsoleLogProvider();
        var endpoint = Activator.CreateInstance(endpointType, provider)!;
        var streamJson = stream == null ? "null" : $"\"{stream}\"";
        SetJsonRequest(endpointType, endpoint,
            $$"""
            {
                "stream": {{streamJson}}
            }
            """);

        var result = endpointType.GetMethod("ExecuteAsync", [typeof(CancellationToken)])!.Invoke(endpoint, [CancellationToken.None]);
        await Assert.IsAssignableFrom<Task>(result);

        Assert.NotNull(provider.LastFilter);
        Assert.Null(provider.LastFilter.Stream);
    }

    [Fact]
    public async Task RecentEndpoint_MapsActivityFiltersToMetadataFilter()
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint", throwOnError: true)!;
        var provider = new TestConsoleLogProvider();
        var endpoint = Activator.CreateInstance(endpointType, provider)!;
        SetJsonRequest(endpointType, endpoint,
            """
            {
                "workflowInstanceId": "workflow-instance-a",
                "activityInstanceId": "activity-instance-a",
                "activityId": "activity-a",
                "activityNodeId": "node-a"
            }
            """);

        var result = endpointType.GetMethod("ExecuteAsync", [typeof(CancellationToken)])!.Invoke(endpoint, [CancellationToken.None]);
        await Assert.IsAssignableFrom<Task>(result);

        Assert.NotNull(provider.LastFilter);
        AssertActivityMetadata(provider.LastFilter.Metadata);
    }

    [Fact]
    public async Task RecentEndpoint_WhenJsonRequestHasUnknownEmptyBody_UsesEmptyFilter()
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint", throwOnError: true)!;
        var provider = new TestConsoleLogProvider();
        var endpoint = Activator.CreateInstance(endpointType, provider)!;
        SetJsonRequest(endpointType, endpoint, "", includeContentLength: false);

        var result = endpointType.GetMethod("ExecuteAsync", [typeof(CancellationToken)])!.Invoke(endpoint, [CancellationToken.None]);
        await Assert.IsAssignableFrom<Task>(result);

        Assert.NotNull(provider.LastFilter);
        Assert.Empty(provider.LastFilter.Metadata);
    }

    [Fact]
    public async Task HubStream_MapsWorkflowInstanceIdToMetadataFilter()
    {
        var provider = new TestConsoleLogProvider();
        var hub = CreateHub(provider, ConsoleLogsPermissions.Read);

        await foreach (var _ in hub.StreamAsync(new ElsaConsoleLogFilter { WorkflowInstanceId = "workflow-instance-a" }, CancellationToken.None))
        {
            // Intentionally consume the stream to trigger provider subscription/filter mapping side effects.
        }

        Assert.NotNull(provider.LastSubscriptionFilter);
        var metadata = provider.LastSubscriptionFilter.Metadata;
        Assert.True(metadata.TryGetValue(ConsoleLogMetadataKeys.WorkflowInstanceId, out var workflowInstanceId));
        Assert.Equal("workflow-instance-a", workflowInstanceId);
    }

    [Fact]
    public async Task HubStream_MapsActivityFiltersToMetadataFilter()
    {
        var provider = new TestConsoleLogProvider();
        var hub = CreateHub(provider, ConsoleLogsPermissions.Read);

        await foreach (var _ in hub.StreamAsync(new ElsaConsoleLogFilter
                       {
                           WorkflowInstanceId = "workflow-instance-a",
                           ActivityInstanceId = "activity-instance-a",
                           ActivityId = "activity-a",
                           ActivityNodeId = "node-a"
                       }, CancellationToken.None))
        {
        }

        Assert.NotNull(provider.LastSubscriptionFilter);
        AssertActivityMetadata(provider.LastSubscriptionFilter.Metadata);
    }

    [Fact]
    public async Task HubSubscribe_MapsWorkflowInstanceIdToMetadataFilter()
    {
        var provider = new TestConsoleLogProvider();
        var hub = CreateHub(provider, ConsoleLogsPermissions.Read);

        await hub.SubscribeAsync(new ElsaConsoleLogFilter { WorkflowInstanceId = "workflow-instance-a" });
        var filter = await provider.WaitForSubscriptionFilterAsync();

        var metadata = filter.Metadata;
        Assert.True(metadata.TryGetValue(ConsoleLogMetadataKeys.WorkflowInstanceId, out var workflowInstanceId));
        Assert.Equal("workflow-instance-a", workflowInstanceId);

        await hub.UnsubscribeAsync();
    }

    private static void AssertActivityMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        Assert.Equal("workflow-instance-a", metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]);
        Assert.Equal("activity-instance-a", metadata[ConsoleLogMetadataKeys.ActivityInstanceId]);
        Assert.Equal("activity-a", metadata[ConsoleLogMetadataKeys.ActivityId]);
        Assert.Equal("node-a", metadata[ConsoleLogMetadataKeys.ActivityNodeId]);
    }

    private static void SetJsonRequest(Type endpointType, object endpoint, string json, bool includeContentLength = true)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(bytes);
        if (includeContentLength)
            context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";

        endpointType
            .GetProperty("HttpContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, context);
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

            if (genericTypeDefinition == typeof(Abstractions.ElsaEndpoint<,>))
                return (genericArguments[0], genericArguments[1]);

            if (genericTypeDefinition == typeof(Abstractions.ElsaEndpoint<,,>))
                return (genericArguments[0], genericArguments[1]);

            if (genericTypeDefinition == typeof(Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), genericArguments[0]);
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
    }

    private static ElsaConsoleLogsHub CreateHub(params string[] permissions)
    {
        return CreateHub(new TestConsoleLogProvider(), permissions);
    }

    private static ElsaConsoleLogsHub CreateHub(TestConsoleLogProvider provider, params string[] permissions)
    {
        var hubContext = new TestHubContext();
        var subscriptionManager = new ElsaConsoleLogSubscriptionManager(provider, hubContext, NullLogger<ElsaConsoleLogSubscriptionManager>.Instance);
        var authorizer = new ElsaConsoleLogStreamHubAuthorizer();

        return new ElsaConsoleLogsHub(provider, authorizer, subscriptionManager)
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
        private readonly TaskCompletionSource<ConsoleLogFilter> _subscriptionFilterSet = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConsoleLogFilter? LastFilter { get; private set; }
        public ConsoleLogFilter? LastSubscriptionFilter { get; private set; }

        public Task<ConsoleLogFilter> WaitForSubscriptionFilterAsync() =>
            _subscriptionFilterSet.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return ValueTask.FromResult(new RecentConsoleLogsResult());
        }

        public IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(
            ConsoleLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            LastSubscriptionFilter = filter;
            _subscriptionFilterSet.TrySetResult(filter);
            return AsyncEnumerable.Empty<ConsoleLogStreamingItem>();
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
        }
    }

    private class TestHubContext : IHubContext<ElsaConsoleLogsHub, IElsaConsoleLogsClient>
    {
        public IHubClients<IElsaConsoleLogsClient> Clients { get; } = new TestHubClients();

        public IGroupManager Groups { get; } = new TestGroupManager();
    }

    private class TestHubClients : IHubClients<IElsaConsoleLogsClient>
    {
        public IElsaConsoleLogsClient All => throw new NotSupportedException();
        public IElsaConsoleLogsClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IElsaConsoleLogsClient Client(string connectionId) => throw new NotSupportedException();
        public IElsaConsoleLogsClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IElsaConsoleLogsClient Group(string groupName) => throw new NotSupportedException();
        public IElsaConsoleLogsClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IElsaConsoleLogsClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IElsaConsoleLogsClient User(string userId) => throw new NotSupportedException();
        public IElsaConsoleLogsClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
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
