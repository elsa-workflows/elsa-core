using System.Reflection;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using NSubstitute;
using WorkflowsApiFeature = Elsa.Workflows.Api.Features.WorkflowsApiFeature;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.RuntimeAdmin;

public class RuntimeAdminAuthorizationTests
{
    [Fact]
    public void StatusEndpoint_AllowsReadWorkflowRuntimePermission()
    {
        var permissions = GetConfiguredPermissions("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Status.StatusEndpoint");

        Assert.Contains(PermissionNames.All, permissions);
        Assert.Contains(PermissionNames.ReadWorkflowRuntime, permissions);
        Assert.Contains(PermissionNames.ManageWorkflowRuntime, permissions);
    }

    [Theory]
    [InlineData("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Pause.PauseEndpoint")]
    [InlineData("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Resume.ResumeEndpoint")]
    [InlineData("Elsa.Workflows.Api.Endpoints.RuntimeAdmin.ForceDrain.ForceDrainEndpoint")]
    public void MutatingEndpoints_RequireManageWorkflowRuntimePermission(string endpointTypeName)
    {
        var permissions = GetConfiguredPermissions(endpointTypeName);

        Assert.Contains(PermissionNames.ManageWorkflowRuntime, permissions);
        Assert.DoesNotContain(PermissionNames.ReadWorkflowRuntime, permissions);
    }

    private static IReadOnlyCollection<string> GetConfiguredPermissions(string endpointTypeName)
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

            if (genericTypeDefinition == typeof(Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), genericArguments[0]);
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
    }
}
