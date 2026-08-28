using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Authorization;
using Elsa.Identity.Permissions;
using Elsa.Identity.Services;
using Elsa.Permissions;
using FastEndpoints;

namespace Elsa.Identity.UnitTests.Authorization;

/// <summary>
/// Pins what the three endpoints that used to carry the <c>SecurityRoot</c> policy require now that the
/// policy is gone, and checks that what they require is something the Identity catalog advertises.
/// </summary>
/// <remarks>
/// Two of the three (<c>Roles/Create</c>, <c>Applications/Create</c>) only lost a redundant policy line and
/// must keep the permission they already declared; <c>Secrets/Hash</c> gained one where it previously had
/// nothing but "any authenticated caller". The coverage gate only asks whether an endpoint declares
/// <em>something</em>, so without these rows either half of that could silently change.
/// </remarks>
public class EndpointPermissionTests
{
    private static readonly Assembly Module = typeof(RoleAuthorizationService).Assembly;

    public static TheoryData<string, string, string> Declarations => new()
    {
        { "Elsa.Identity.Endpoints.Secrets.Hash.Hash", IdentityPermissions.Users, CoreVerbs.Create },
        { "Elsa.Identity.Endpoints.Roles.Create.Create", IdentityPermissions.Roles, CoreVerbs.Create },
        { "Elsa.Identity.Endpoints.Applications.Create.Create", IdentityPermissions.Applications, CoreVerbs.Create }
    };

    [Theory]
    [MemberData(nameof(Declarations))]
    public void EndpointDeclaresItsExpectedPermission(string endpointTypeName, string resource, string verb) =>
        Assert.Equal(new Permission(resource, verb), Declare(endpointTypeName));

    [Theory]
    [MemberData(nameof(Declarations))]
    public void EveryDeclaredPermissionIsAdvertisedByTheCatalog(string endpointTypeName, string resource, string verb)
    {
        var declared = Declare(endpointTypeName);
        var descriptor = new IdentityPermissionsDescriptorProvider().GetDescriptors().SingleOrDefault(x => x.Resource == declared.Resource);

        Assert.True(descriptor is not null, $"{endpointTypeName} requires resource '{declared.Resource}', which the module contributes no descriptor for, so it cannot be granted through the role editor.");
        Assert.True(descriptor!.Supports(declared.Verb), $"{endpointTypeName} requires '{declared}', but '{declared.Resource}' advertises only [{string.Join(", ", descriptor.SupportedVerbs)}].");
        Assert.Equal(new Permission(resource, verb), declared);
    }

    /// <summary>
    /// Runs one endpoint's <c>Configure()</c> and returns what it recorded. The requirement is attached as an
    /// inline policy, which cannot be read back off the definition, so the registry is the only way to observe
    /// a declaration without booting a host. The instance skips its constructor because <c>Configure()</c>
    /// touches none of the injected services, and substituting them would make the rows depend on which
    /// dependencies happen to be interfaces.
    /// </summary>
    private static Permission Declare(string endpointTypeName)
    {
        var endpointType = Module.GetType(endpointTypeName, true)!;
        var endpoint = RuntimeHelpers.GetUninitializedObject(endpointType);
        var (requestType, responseType) = DtoTypes(endpointType);

        endpointType.GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, new EndpointDefinition(endpointType, requestType, responseType));
        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        var permission = EndpointPermissionRegistry.Find(endpointType);

        Assert.True(permission.HasValue, $"{endpointTypeName} declares no permission.");
        return permission!.Value;
    }

    private static (Type Request, Type Response) DtoTypes(Type endpointType)
    {
        for (var type = endpointType.BaseType; type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType)
                continue;

            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();

            if (definition == typeof(Elsa.Abstractions.ElsaEndpoint<,>))
                return (arguments[0], arguments[1]);
            if (definition == typeof(Elsa.Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), arguments[0]);
            if (definition == typeof(Elsa.Abstractions.ElsaEndpoint<>))
                return (arguments[0], typeof(object));
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
    }
}
