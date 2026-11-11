using System.Reflection;
using Elsa.Authorization;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Features;
using Elsa.Diagnostics.StructuredLogs.Models;
using FastEndpoints;

namespace Elsa.Diagnostics.StructuredLogs.IntegrationTests;

public class StructuredLogsEndpointAuthorizationTests
{
    [Theory]
    [InlineData("Elsa.Diagnostics.StructuredLogs.Endpoints.StructuredLogs.Recent.Endpoint", typeof(StructuredLogFilter), typeof(RecentStructuredLogsResult))]
    [InlineData("Elsa.Diagnostics.StructuredLogs.Endpoints.StructuredLogs.Sources.Endpoint", typeof(EmptyRequest), typeof(IReadOnlyCollection<StructuredLogSource>))]
    [InlineData("Elsa.Diagnostics.StructuredLogs.Endpoints.StructuredLogs.Storage.Endpoint", typeof(EmptyRequest), typeof(StructuredLogStorageDiagnostics))]
    public void RestEndpoints_RequireStructuredLogsViewPermission(string endpointTypeName, Type requestDtoType, Type responseDtoType)
    {
        var endpointType = typeof(StructuredLogsFeature).Assembly.GetType(endpointTypeName, throwOnError: true)!;
        var constructorArgument = endpointType.GetConstructors().Single().GetParameters().Single().ParameterType == typeof(IStructuredLogProvider)
            ? (object)new NoopProvider()
            : Array.Empty<IStructuredLogStorageDiagnostics>();
        var endpoint = Activator.CreateInstance(endpointType, constructorArgument)!;

        endpointType
            .GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, new EndpointDefinition(endpointType, requestDtoType, responseDtoType));
        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        var permission = EndpointPermissionRegistry.Find(endpointType);

        Assert.True(permission.HasValue, $"{endpointTypeName} declares no permission.");
        Assert.Equal("diagnostics/structured-logs:view", permission!.Value.ToString());
    }

    private sealed class NoopProvider : IStructuredLogProvider
    {
        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
