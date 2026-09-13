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
    [Test]
    public async Task HubSubscribe_WithoutConsoleLogsPermission_DeniesAccess()
    {
        using var hubScope = CreateHub("diagnostics/console-logs:write");
        var hub = hubScope.Hub;

        await Assert.ThrowsExactlyAsync<HubException>(() => hub.SubscribeAsync(new()));
    }

    [Test]
    public async Task HubUpdateFilter_WithoutConsoleLogsPermission_DeniesAccess()
    {
        using var hubScope = CreateHub("diagnostics/console-logs:write");
        var hub = hubScope.Hub;

        await Assert.ThrowsExactlyAsync<HubException>(() => hub.UpdateFilterAsync(new()));
    }

    [Test]
    [Arguments("diagnostics/console-logs:view")]
    [Arguments(PermissionNames.All)]
    [Arguments("*:view")]
    public async Task HubSubscribe_WithConsoleLogsPermission_AllowsAccess(string permission)
    {
        using var hubScope = CreateHub(permission);
        var hub = hubScope.Hub;

        await hub.SubscribeAsync(new());
    }

    [Test]
    [Arguments("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint")]
    [Arguments("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Sources.Endpoint")]
    public async Task RestEndpoints_RequireConsoleLogsPermission(string endpointTypeName)
    {
        var permissions = await GetConfiguredPermissionsAsync(endpointTypeName);

        await Assert.That(permissions).Contains("diagnostics/console-logs:view");
    }

    [Test]
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
        await Assert.That(result).IsAssignableTo<Task>();
        await (Task)result!;

        var filter = (await Assert.That(provider.LastFilter).IsNotNull())!;
        var metadata = filter.Metadata;
        await Assert.That(metadata.TryGetValue(ConsoleLogMetadataKeys.WorkflowInstanceId, out var workflowInstanceId)).IsTrue();
        await Assert.That(workflowInstanceId).IsEqualTo("workflow-instance-a");
    }

    [Test]
    [Arguments("stdout", ConsoleStream.Stdout)]
    [Arguments("stderr", ConsoleStream.Stderr)]
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
        await Assert.That(result).IsAssignableTo<Task>();
        await (Task)result!;

        var filter = (await Assert.That(provider.LastFilter).IsNotNull())!;
        await Assert.That(filter.Stream).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("all")]
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
        await Assert.That(result).IsAssignableTo<Task>();
        await (Task)result!;

        var filter = (await Assert.That(provider.LastFilter).IsNotNull())!;
        await Assert.That(filter.Stream).IsNull();
    }

    [Test]
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
        await Assert.That(result).IsAssignableTo<Task>();
        await (Task)result!;

        var filter = (await Assert.That(provider.LastFilter).IsNotNull())!;
        await AssertActivityMetadataAsync(filter.Metadata);
    }

    [Test]
    public async Task RecentEndpoint_WhenJsonRequestHasUnknownEmptyBody_UsesEmptyFilter()
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint", throwOnError: true)!;
        var provider = new TestConsoleLogProvider();
        var endpoint = Activator.CreateInstance(endpointType, provider)!;
        SetJsonRequest(endpointType, endpoint, "", includeContentLength: false);

        var result = endpointType.GetMethod("ExecuteAsync", [typeof(CancellationToken)])!.Invoke(endpoint, [CancellationToken.None]);
        await Assert.That(result).IsAssignableTo<Task>();
        await (Task)result!;

        var filter = (await Assert.That(provider.LastFilter).IsNotNull())!;
        await Assert.That(filter.Metadata).IsEmpty();
    }

    [Test]
    public async Task HubStream_MapsWorkflowInstanceIdToMetadataFilter()
    {
        var provider = new TestConsoleLogProvider();
        using var hubScope = CreateHub(provider, "diagnostics/console-logs:view");
        var hub = hubScope.Hub;

        await foreach (var _ in hub.StreamAsync(new ElsaConsoleLogFilter { WorkflowInstanceId = "workflow-instance-a" }, CancellationToken.None))
        {
            // Intentionally consume the stream to trigger provider subscription/filter mapping side effects.
        }

        var filter = (await Assert.That(provider.LastSubscriptionFilter).IsNotNull())!;
        var metadata = filter.Metadata;
        await Assert.That(metadata.TryGetValue(ConsoleLogMetadataKeys.WorkflowInstanceId, out var workflowInstanceId)).IsTrue();
        await Assert.That(workflowInstanceId).IsEqualTo("workflow-instance-a");
    }

    [Test]
    public async Task HubStream_MapsActivityFiltersToMetadataFilter()
    {
        var provider = new TestConsoleLogProvider();
        using var hubScope = CreateHub(provider, "diagnostics/console-logs:view");
        var hub = hubScope.Hub;

        await foreach (var _ in hub.StreamAsync(new ElsaConsoleLogFilter
                       {
                           WorkflowInstanceId = "workflow-instance-a",
                           ActivityInstanceId = "activity-instance-a",
                           ActivityId = "activity-a",
                           ActivityNodeId = "node-a"
                       }, CancellationToken.None))
        {
        }

        var filter = (await Assert.That(provider.LastSubscriptionFilter).IsNotNull())!;
        await AssertActivityMetadataAsync(filter.Metadata);
    }

    [Test]
    public async Task HubSubscribe_MapsWorkflowInstanceIdToMetadataFilter()
    {
        var provider = new TestConsoleLogProvider();
        using var hubScope = CreateHub(provider, "diagnostics/console-logs:view");
        var hub = hubScope.Hub;

        await hub.SubscribeAsync(new ElsaConsoleLogFilter { WorkflowInstanceId = "workflow-instance-a" });
        var filter = await provider.WaitForSubscriptionFilterAsync();

        var metadata = filter.Metadata;
        await Assert.That(metadata.TryGetValue(ConsoleLogMetadataKeys.WorkflowInstanceId, out var workflowInstanceId)).IsTrue();
        await Assert.That(workflowInstanceId).IsEqualTo("workflow-instance-a");

        await hub.UnsubscribeAsync();
    }

    private static async Task AssertActivityMetadataAsync(IReadOnlyDictionary<string, string> metadata)
    {
        await Assert.That(metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]).IsEqualTo("workflow-instance-a");
        await Assert.That(metadata[ConsoleLogMetadataKeys.ActivityInstanceId]).IsEqualTo("activity-instance-a");
        await Assert.That(metadata[ConsoleLogMetadataKeys.ActivityId]).IsEqualTo("activity-a");
        await Assert.That(metadata[ConsoleLogMetadataKeys.ActivityNodeId]).IsEqualTo("node-a");
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

    private static async Task<IReadOnlyCollection<string>> GetConfiguredPermissionsAsync(string endpointTypeName)
    {
        var endpointType = typeof(ConsoleLogsFeature).Assembly.GetType(endpointTypeName, throwOnError: true)!;
        var endpoint = Activator.CreateInstance(endpointType, new TestConsoleLogProvider())!;
        var (requestDtoType, responseDtoType) = GetEndpointDtoTypes(endpointType);
        var definition = new EndpointDefinition(endpointType, requestDtoType, responseDtoType);

        endpointType
            .GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, definition);

        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        // The requirement is attached as an inline policy, not to AllowedPermissions, so the declaration is
        // read back from the registry that records it.
        var permission = Elsa.Authorization.EndpointPermissionRegistry.Find(endpointType);

        await Assert.That(permission.HasValue).IsTrue().Because($"{endpointTypeName} declares no permission.");

        return [permission!.Value.ToString()];
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

    private static HubTestScope CreateHub(params string[] permissions)
    {
        return CreateHub(new TestConsoleLogProvider(), permissions);
    }

    private static HubTestScope CreateHub(TestConsoleLogProvider provider, params string[] permissions)
    {
        var hubContext = new TestHubContext();
        var subscriptionManager = new ElsaConsoleLogSubscriptionManager(provider, new TestConsoleLogSourceRegistry(), hubContext, NullLogger<ElsaConsoleLogSubscriptionManager>.Instance);
        var authorizer = new ElsaConsoleLogStreamHubAuthorizer();

        var hub = new ElsaConsoleLogsHub(provider, authorizer, subscriptionManager)
        {
            Context = new TestHubCallerContext(CreateUser(permissions))
        };

        return new HubTestScope(hub, subscriptionManager);
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

    private sealed class HubTestScope(ElsaConsoleLogsHub hub, ElsaConsoleLogSubscriptionManager subscriptionManager) : IDisposable
    {
        public ElsaConsoleLogsHub Hub { get; } = hub;

        public void Dispose()
        {
            subscriptionManager.Dispose();
        }
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
