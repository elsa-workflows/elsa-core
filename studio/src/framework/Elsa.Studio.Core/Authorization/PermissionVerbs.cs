namespace Elsa.Studio.Authorization;

/// <summary>The backend's recommended core verbs.</summary>
public static class PermissionVerbs
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
}
