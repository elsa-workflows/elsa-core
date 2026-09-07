using Elsa.Authorization;

namespace Elsa.Permissions;

/// <summary>Why one submitted permission was rejected.</summary>
public record PermissionGrantError(string Permission, string Reason);

/// <summary>The outcome of validating a set of submitted permissions.</summary>
public record PermissionGrantValidationResult(IReadOnlyCollection<PermissionGrantError> Errors)
{
    /// <summary>Whether every submitted permission was acceptable.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>An accepted result.</summary>
    public static PermissionGrantValidationResult Valid { get; } = new([]);
}

/// <summary>Validates permissions submitted when authoring a role, before they are persisted.</summary>
public interface IPermissionGrantValidator
{
    /// <summary>Validates <paramref name="permissions"/> against the catalog.</summary>
    PermissionGrantValidationResult Validate(IEnumerable<string>? permissions);
}

/// <inheritdoc />
/// <remarks>
/// Concrete segments are validated against the registry; wildcard segments are validated structurally
/// only. A wildcard is deliberately accepted **even when it currently matches nothing** — a grant naming
/// a module that is not installed yet must survive, because installing that module later is exactly what
/// gives the grant meaning. Validating wildcards against the catalog would reject <c>workflows/*:view</c>,
/// which is the grant the hierarchy exists to make possible.
/// </remarks>
public sealed class PermissionGrantValidator(IPermissionDescriptorRegistry registry) : IPermissionGrantValidator
{
    /// <inheritdoc />
    public PermissionGrantValidationResult Validate(IEnumerable<string>? permissions)
    {
        if (permissions is null)
            return PermissionGrantValidationResult.Valid;

        var errors = new List<PermissionGrantError>();

        foreach (var value in permissions.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
        {
            if (!Permission.TryParse(value, out var permission))
            {
                errors.Add(new(value, "Not a well-formed permission. Expected '{resource}:{verb}'."));
                continue;
            }

            // An entry like 'workflows*:delete' parses but the matcher never satisfies it, so it would
            // be persisted as a grant that silently reaches nothing.
            if (!permission.IsValidPattern)
            {
                errors.Add(new(value, "Places '*' where it has no meaning and would match nothing. A wildcard may only be the entire resource ('*'), a trailing '/*' segment ('workflows/*'), or the entire verb ('workflows/definitions:*')."));
                continue;
            }

            if (permission.IsResourceWildcard || permission.IsSubtree)
                continue;

            var descriptor = registry.Find(permission.Resource);

            if (descriptor is null)
            {
                errors.Add(new(value, $"No module registers the resource '{permission.Resource}'."));
                continue;
            }

            if (permission.IsVerbWildcard)
                continue;

            if (!descriptor.Supports(permission.Verb))
                errors.Add(new(value, $"The resource '{permission.Resource}' does not support the verb '{permission.Verb}'. Supported: {string.Join(", ", descriptor.SupportedVerbs)}."));
        }

        return errors.Count == 0 ? PermissionGrantValidationResult.Valid : new(errors);
    }
}
