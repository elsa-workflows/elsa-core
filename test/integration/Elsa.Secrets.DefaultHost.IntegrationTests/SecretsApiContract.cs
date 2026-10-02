using System.Text.Json;
using System.Text.RegularExpressions;
using FastEndpoints;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Secrets.DefaultHost.IntegrationTests;

/// <summary>
/// The Core and legacy Secrets route sets and legacy-only package names pinned in
/// <c>scripts/integration-program/secrets-api-studio-contract.json</c>, which is copied next to this assembly.
/// </summary>
public sealed record SecretsApiContract(
    IReadOnlyList<ContractRoute> CoreRoutes,
    IReadOnlyList<ContractRoute> LegacyRoutes,
    IReadOnlyList<string> LegacyOnlyPackages)
{
    public const string CoreAssembly = "Elsa.Secrets";

    public static SecretsApiContract Pinned { get; } = JsonSerializer.Deserialize<SecretsApiContract>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "secrets-api-studio-contract.json")), JsonSerializerOptions.Web)!;

    /// <summary>
    /// The verb and template with parameter names erased, as <c>normalize_route</c> in the contract verifier does, so a
    /// legacy <c>{id}</c> route collides with the Core <c>{name}</c> route it would shadow.
    /// </summary>
    public static string Key(string verb, string path) =>
        $"{verb.ToUpperInvariant()} {Regex.Replace(path, @"\{[^{}]+\}", "{}").ToLowerInvariant()}";

    /// <summary>
    /// Whether the route belongs to a Secrets route family of the contract (<c>/secrets</c>, and the legacy
    /// <c>/actions/secrets</c>, <c>/bulk-actions/secrets</c> and <c>/queries/secrets</c>), the families the live route
    /// probe inspected.
    /// </summary>
    public bool IsSecretsRoute(RegisteredRoute route) => CoreRoutes.Concat(LegacyRoutes)
        .Select(contractRoute => contractRoute.Path[..(contractRoute.Path.IndexOf("/secrets", StringComparison.Ordinal) + "/secrets".Length)])
        .Any(family => route.Path.Equals(family, StringComparison.OrdinalIgnoreCase)
                       || route.Path.StartsWith(family + "/", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Every way the registered Secrets routes differ from exactly one <see cref="CoreAssembly"/> endpoint per canonical
    /// route and nothing else, keyed by <see cref="Key"/>.
    /// </summary>
    public IReadOnlyList<RouteViolation> Violations(IEnumerable<RegisteredRoute> routes)
    {
        var canonical = CoreRoutes.ToDictionary(route => Key(route.Verb, route.Path));
        var registered = routes.Where(IsSecretsRoute).ToLookup(route => Key(route.Verb, route.Path));
        var violations = new List<RouteViolation>();

        foreach (var (key, route) in canonical)
        {
            var endpoints = registered[key].ToList();
            if (endpoints is not [{ HandlerAssembly: CoreAssembly } endpoint] || endpoint.Path != route.Path)
            {
                violations.Add(new(key, $"expected one {CoreAssembly} endpoint at {route.Path}, found [{string.Join(", ", endpoints)}]"));
            }
        }

        violations.AddRange(registered
            .Where(endpoints => !canonical.ContainsKey(endpoints.Key))
            .Select(endpoints => new RouteViolation(endpoints.Key, $"not a canonical Core route: [{string.Join(", ", endpoints)}]")));
        return violations;
    }
}

public sealed record ContractRoute(string Verb, string Path);

public sealed record RouteViolation(string RouteKey, string Problem);

/// <summary>One verb of a registered endpoint, with the Elsa API prefix removed, and the assembly of its FastEndpoints type.</summary>
/// <remarks>An endpoint that is not a FastEndpoints endpoint has no handler assembly, so it can never pass as Core-owned.</remarks>
public sealed record RegisteredRoute(string Verb, string Path, string? HandlerAssembly)
{
    public static IEnumerable<RegisteredRoute> From(RouteEndpoint endpoint, string routePrefix)
    {
        var template = endpoint.RoutePattern.RawText ?? throw new InvalidOperationException($"Endpoint '{endpoint.DisplayName}' has no route template.");
        var path = "/" + template.TrimStart('/');
        if (path.StartsWith(routePrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            path = path[routePrefix.Length..];
        }

        var handlerAssembly = endpoint.Metadata.GetMetadata<EndpointDefinition>()?.EndpointType.Assembly.GetName().Name;
        // An endpoint without method metadata answers every verb, so it is recorded rather than dropped.
        var verbs = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"];
        return verbs.Select(verb => new RegisteredRoute(verb, path, handlerAssembly));
    }
}
