using System.Reflection;
using Xunit;

namespace Elsa.Testing.Shared.Authorization;

/// <summary>
/// The fail-closed endpoint gate, as a reusable assertion. Omitting a declaration inherits the
/// FastEndpoints default with no Elsa-level fallback, so an endpoint can reach production ungated with
/// nobody noticing. This asserts every endpoint in an assembly states its access explicitly.
/// </summary>
/// <remarks>
/// Reflection over <c>Configure()</c> is deliberate. Booting a host would prove more, but would require
/// every endpoint-bearing module as a dependency of one test project, which is what stopped such a gate
/// existing. A module opts in with a single test calling <see cref="AssertEveryEndpointDeclaresAccess"/>.
/// </remarks>
public static class EndpointCoverage
{
    // The declaration helpers are protected, so they are matched by name.
    private static readonly string[] Declarations =
    [
        "RequirePermission",
        "RequireAuthenticatedOnly",
        "ConfigurePermissions",
        "AllowAnonymous"
    ];

    /// <summary>Asserts every Elsa endpoint in <paramref name="assembly"/> declares its access.</summary>
    public static void AssertEveryEndpointDeclaresAccess(Assembly assembly)
    {
        var endpoints = FindEndpoints(assembly).ToArray();

        // A reflection gate that silently matches nothing passes forever.
        Assert.True(endpoints.Length > 0, $"No Elsa endpoints found in {assembly.GetName().Name}. The gate is not looking where it thinks it is.");

        var undeclared = endpoints.Where(x => !DeclaresAccess(x)).Select(x => x.FullName).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.True(
            undeclared.Length == 0,
            $"{undeclared.Length} endpoint(s) declare no access: {string.Join(", ", undeclared)}. "
            + "Every endpoint must call exactly one of RequirePermission, RequireAuthenticatedOnly, or AllowAnonymous. "
            + "There is no exemption list: an endpoint that states nothing is indistinguishable from one whose author forgot.");
    }

    /// <summary>The Elsa endpoint types declared in <paramref name="assembly"/>.</summary>
    public static IEnumerable<Type> FindEndpoints(Assembly assembly)
    {
        Type[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(x => x is not null).ToArray()!;
        }

        return types.Where(IsEndpoint);
    }

    private static bool IsEndpoint(Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false })
            return false;

        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.FullName?.StartsWith("Elsa.Abstractions.ElsaEndpoint", StringComparison.Ordinal) == true)
                return true;
        }

        return false;
    }

    private static bool DeclaresAccess(Type endpointType)
    {
        var configure = endpointType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // An endpoint inheriting Configure from an abstract base declares through that base.
        if (configure is null)
            return endpointType.BaseType is not null && endpointType.BaseType.IsAbstract && DeclaresAccessOnAnyBase(endpointType.BaseType);

        return ReferencesDeclaration(configure);
    }

    private static bool DeclaresAccessOnAnyBase(Type baseType)
    {
        for (var type = baseType; type is not null; type = type.BaseType)
        {
            var configure = type.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (configure is not null && ReferencesDeclaration(configure))
                return true;
        }

        return false;
    }

    private static bool ReferencesDeclaration(MethodInfo configure)
    {
        var body = configure.GetMethodBody();

        if (body is null)
            return false;

        var module = configure.Module;
        var il = body.GetILAsByteArray() ?? [];

        for (var i = 0; i < il.Length - 4; i++)
        {
            // 0x28 call, 0x6F callvirt.
            if (il[i] is not (0x28 or 0x6F))
                continue;

            try
            {
                var called = module.ResolveMethod(BitConverter.ToInt32(il, i + 1));

                if (called is not null && Declarations.Contains(called.Name, StringComparer.Ordinal))
                    return true;
            }
            catch
            {
                // Not a method token at this offset; keep scanning.
            }
        }

        return false;
    }
}
