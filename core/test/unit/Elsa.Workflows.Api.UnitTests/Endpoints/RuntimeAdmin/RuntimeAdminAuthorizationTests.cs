using System.Reflection;
using Elsa.Authorization;
using Elsa.Workflows.Api.Permissions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using NSubstitute;
using WorkflowsApiFeature = Elsa.Workflows.Api.Features.WorkflowsApiFeature;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.RuntimeAdmin;

public class RuntimeAdminAuthorizationTests
{
    [Fact]
    public void StatusEndpoint_RequiresOnlyViewOnTheRuntime()
    {
        // Reading runtime status must not require the verb that pauses or drains it.
        var permission = GetDeclaredPermission("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Status.StatusEndpoint");

        Assert.Equal(WorkflowPermissions.Runtime, permission.Resource);
        Assert.Equal(CoreVerbs.View, permission.Verb);
    }

    [Theory]
    [InlineData("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Pause.PauseEndpoint")]
    [InlineData("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Resume.ResumeEndpoint")]
    [InlineData("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.ForceDrain.ForceDrainEndpoint")]
    public void MutatingEndpoints_RequireControlOnTheRuntime(string endpointTypeName)
    {
        var permission = GetDeclaredPermission(endpointTypeName);

        Assert.Equal(WorkflowPermissions.Runtime, permission.Resource);
        Assert.Equal("control", permission.Verb);
        Assert.NotEqual(CoreVerbs.View, permission.Verb);
    }

    private static Permission GetDeclaredPermission(string endpointTypeName)
    {
        var endpointType = typeof(WorkflowsApiFeature).Assembly.GetType(endpointTypeName, throwOnError: true)!;
        var endpoint = Activator.CreateInstance(
            endpointType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [Substitute.For<IWorkflowRuntimeAdminService>()],
            null)!;
        var (requestDtoType, responseDtoType) = GetEndpointDtoTypes(endpointType);
        var definition = new EndpointDefinition(endpointType, requestDtoType, responseDtoType);

        endpointType
            .GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, definition);

        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        var permission = EndpointPermissionRegistry.Find(endpointType);

        Assert.True(permission.HasValue, $"{endpointTypeName} declares no permission.");

        return permission!.Value;
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

            if (genericTypeDefinition == typeof(Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), genericArguments[0]);
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
    }
}
