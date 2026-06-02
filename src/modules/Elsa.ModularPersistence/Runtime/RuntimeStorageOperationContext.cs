namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeStorageOperationContext
{
    public RuntimeStorageOperationContext(string actor, IEnumerable<string>? permissions = null)
    {
        Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();
        Permissions = (permissions ?? []).ToHashSet(StringComparer.Ordinal);
    }

    public string Actor { get; }

    public IReadOnlySet<string> Permissions { get; }

    public static RuntimeStorageOperationContext System { get; } = new("system", [PermissionNames.All]);
}
