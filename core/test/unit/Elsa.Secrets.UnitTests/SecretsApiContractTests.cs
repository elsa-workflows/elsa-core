using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Secrets.Features;
using Elsa.Secrets.Models;
using Elsa.Secrets.Permissions;
using FastEndpoints;

namespace Elsa.Secrets.UnitTests;

public class SecretsApiContractTests
{
    private static readonly (string Endpoint, string Verb, string Route, string Permission)[] ExpectedEndpoints =
    [
        ("Create", "POST", "/secrets", "secrets:write"),
        ("Delete", "DELETE", "/secrets/{name}", "secrets:delete"),
        ("Descriptors", "GET", "/secrets/descriptors", "secrets:view"),
        ("Get", "GET", "/secrets/{name}", "secrets:view"),
        ("List", "GET", "/secrets", "secrets:view"),
        ("Picker", "POST", "/secrets/picker", "secrets:view"),
        ("Revoke", "POST", "/secrets/{name}/revoke", "secrets:write"),
        ("Rotate", "POST", "/secrets/{name}/rotate", "secrets:write"),
        ("Test", "POST", "/secrets/{name}/test", "secrets:test"),
        ("Update", "POST", "/secrets/{name}", "secrets:write")
    ];

    [Fact]
    public void CoreEndpointAssemblyHasOneCanonicalSecretsRouteAndPermissionPerOperation()
    {
        var actual = DescribeCoreSecretsEndpoints();

        Assert.Equal(ExpectedEndpoints.OrderBy(x => x.Endpoint), actual.OrderBy(x => x.Endpoint));
        Assert.DoesNotContain(actual, x => x.Route.EndsWith("/input", StringComparison.Ordinal));
        Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name == "Elsa.Secrets.Api");
    }

    [Fact]
    public void CoreSecretsPermissionDescriptorAdvertisesEveryDeclaredVerb()
    {
        var descriptor = new SecretsResourcePermissionsDescriptorProvider()
            .GetDescriptors()
            .Single(x => x.Resource == SecretsResourcePermissions.Secrets);

        foreach (var permission in ExpectedEndpoints.Select(x => Permission.Parse(x.Permission)).Distinct())
        {
            Assert.True(descriptor.Supports(permission.Verb), $"The Secrets permission catalog does not advertise '{permission.Verb}'.");
        }
    }

    [Fact]
    public void LegacyReadTokenDoesNotAuthorizeCoreViewAndWriteDoesNotImplyDelete()
    {
        var evaluator = new PermissionEvaluator();
        var legacyReadPrincipal = PrincipalWith("secrets:read");
        var writeOnlyPrincipal = PrincipalWith("secrets:write");

        Assert.False(evaluator.HasPermission(legacyReadPrincipal, SecretsResourcePermissions.Secrets, CoreVerbs.View));
        Assert.True(evaluator.HasPermission(writeOnlyPrincipal, SecretsResourcePermissions.Secrets, CoreVerbs.Write));
        Assert.False(evaluator.HasPermission(writeOnlyPrincipal, SecretsResourcePermissions.Secrets, CoreVerbs.Delete));
    }

    [Fact]
    public void CoreSecretResponseDoesNotExposeStoredValue()
    {
        var propertyNames = typeof(SecretModel).GetProperties().Select(x => x.Name);

        Assert.DoesNotContain(propertyNames, x => string.Equals(x, "Value", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, x => string.Equals(x, "EncryptedValue", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, x => string.Equals(x, "ProtectedValue", StringComparison.OrdinalIgnoreCase));
    }

    private static ClaimsPrincipal PrincipalWith(string permission) =>
        new(new ClaimsIdentity([new Claim(PermissionNames.ClaimType, permission)], "contract-test"));

    private static IReadOnlyList<(string Endpoint, string Verb, string Route, string Permission)> DescribeCoreSecretsEndpoints()
    {
        var assembly = typeof(SecretsFeature).Assembly;
        var endpointTypes = assembly.GetTypes()
            .Where(type => type.IsClass
                && type.Namespace?.StartsWith("Elsa.Secrets.Endpoints.Secrets.", StringComparison.Ordinal) == true
                && type.GetMethod("Configure", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.DeclaringType == type)
            .ToArray();

        var results = new List<(string Endpoint, string Verb, string Route, string Permission)>();
        foreach (var endpointType in endpointTypes)
        {
            var endpoint = RuntimeHelpers.GetUninitializedObject(endpointType);
            var (requestType, responseType) = GetDtoTypes(endpointType);
            var definition = new EndpointDefinition(endpointType, requestType, responseType);
            endpointType.GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(endpoint, definition);
            endpointType.GetMethod("Configure", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .Invoke(endpoint, null);

            var permission = EndpointPermissionRegistry.Find(endpointType);
            Assert.True(permission.HasValue, $"{endpointType.FullName} declares no Elsa permission.");

            foreach (var route in definition.Routes.SelectMany(route => definition.Verbs.Select(verb => (verb, route))))
            {
                results.Add((endpointType.Namespace!.Split('.').Last(), route.verb, route.route, permission.Value.ToString()));
            }
        }

        return results;
    }

    private static (Type Request, Type Response) GetDtoTypes(Type endpointType)
    {
        for (var type = endpointType; type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType)
            {
                if (type == typeof(ElsaEndpointWithoutRequest))
                {
                    return (typeof(EmptyRequest), typeof(object));
                }

                continue;
            }

            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            if (definition == typeof(ElsaEndpoint<,>))
            {
                return (arguments[0], arguments[1]);
            }

            if (definition == typeof(ElsaEndpointWithoutRequest<>))
            {
                return (typeof(EmptyRequest), arguments[0]);
            }

            if (definition == typeof(ElsaEndpoint<>))
            {
                return (arguments[0], typeof(object));
            }
        }

        throw new InvalidOperationException($"Unsupported Secrets endpoint type '{endpointType.FullName}'.");
    }
}
