namespace Elsa.Authorization;

/// <summary>
/// The recommended core verbs. Modules SHOULD reuse these wherever the meaning fits, so that a role
/// editor reads consistently across resources. They are a convention, not a closed set: a module that
/// needs a verb outside this list declares it, and the catalog marks it as non-core so a reviewer can
/// spot a needless synonym.
/// </summary>
/// <remarks>
/// A resource declares either <see cref="Create"/> + <see cref="Update"/>, or <see cref="Write"/> —
/// never both. Which one depends on whether the module's API separates the operations. Never-both is
/// what stops <see cref="Write"/> acting as an aggregate: within any one resource there is no ambiguity
/// about which verb an endpoint requires, so no verb needs to imply another.
/// </remarks>
public static class CoreVerbs
{
    /// <summary>Read, list, query, inspect, export.</summary>
    public const string View = "view";

    /// <summary>Bring a new record into existence.</summary>
    public const string Create = "create";

    /// <summary>Modify an existing record.</summary>
    public const string Update = "update";

    /// <summary>Create or modify, where the API does not separate the two.</summary>
    public const string Write = "write";

    /// <summary>Remove a record.</summary>
    public const string Delete = "delete";

    /// <summary>Run, dispatch, or invoke against a live system.</summary>
    public const string Execute = "execute";

    /// <summary>The recommended core set, in declaration order.</summary>
    public static IReadOnlyList<string> All { get; } = [View, Create, Update, Write, Delete, Execute];

    /// <summary>Whether <paramref name="verb"/> belongs to the recommended core set.</summary>
    public static bool IsCore(string verb) => All.Contains(verb, StringComparer.Ordinal);
}
