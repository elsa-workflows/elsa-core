using System.Reflection;
using Elsa.Studio.Security.Client;
using Refit;
using Xunit;

namespace Elsa.Studio.Security.Tests;

/// <summary>
/// Keeps the Studio client contracts aligned with the Identity endpoints. These assertions intentionally inspect
/// Refit metadata, because a route typo can otherwise remain hidden until the first screen is used.
/// </summary>
public sealed class SecurityApiContractTests
{
    public static TheoryData<Type, string, string, string> EndpointContracts => new()
    {
        { typeof(IRolesApi), nameof(IRolesApi.ListAsync), "GET", "/identity/roles" },
        { typeof(IRolesApi), nameof(IRolesApi.CreateAsync), "POST", "/identity/roles" },
        { typeof(IRolesApi), nameof(IRolesApi.UpdateAsync), "PUT", "/identity/roles/{id}" },
        { typeof(IRolesApi), nameof(IRolesApi.DeleteAsync), "DELETE", "/identity/roles/{id}" },
        { typeof(IRolesApi), nameof(IRolesApi.GetDeletionImpactAsync), "GET", "/identity/roles/{id}/deletion-impact" },
        { typeof(IRolesApi), nameof(IRolesApi.RemediateAndDeleteAsync), "POST", "/identity/roles/{id}/remove-from-jit-policies-and-delete" },
        { typeof(IPermissionsApi), nameof(IPermissionsApi.ListAsync), "GET", "/identity/permissions" },
        { typeof(IPermissionsApi), nameof(IPermissionsApi.GetReachAsync), "GET", "/identity/permissions/reach" },
        { typeof(IMePermissionsApi), nameof(IMePermissionsApi.GetAsync), "GET", "/identity/me/permissions" }
    };

    [Theory]
    [MemberData(nameof(EndpointContracts))]
    public void IdentityEndpointsExposeTheApprovedRefitContract(Type apiType, string methodName, string httpMethod, string path)
    {
        var method = apiType.GetMethod(methodName)!;
        var attribute = method.GetCustomAttribute<HttpMethodAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(httpMethod, attribute!.Method.Method);
        Assert.Equal(path, attribute.Path);
    }

    [Fact]
    public void ReachEndpointBindsResourceAsAQueryParameter()
    {
        var method = typeof(IPermissionsApi).GetMethod(nameof(IPermissionsApi.GetReachAsync))!;
        var resource = Assert.Single(method.GetParameters(), parameter => parameter.Name == "resource");

        Assert.NotNull(resource.GetCustomAttribute<QueryAttribute>());
        Assert.Equal(typeof(string), resource.ParameterType);
    }

    [Theory]
    [InlineData(typeof(IRolesApi), nameof(IRolesApi.CreateAsync), 0)]
    [InlineData(typeof(IRolesApi), nameof(IRolesApi.UpdateAsync), 1)]
    [InlineData(typeof(IRolesApi), nameof(IRolesApi.RemediateAndDeleteAsync), 1)]
    public void MutatingEndpointsMarkTheirRequestAsTheRefitBody(Type apiType, string methodName, int bodyParameterIndex)
    {
        var parameter = apiType.GetMethod(methodName)!.GetParameters()[bodyParameterIndex];

        Assert.NotNull(parameter.GetCustomAttribute<BodyAttribute>());
    }
}
