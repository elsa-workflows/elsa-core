namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.BulkDelete;

/// <summary>
/// Represents a bulk-delete activity definitions request.
/// </summary>
public class Request
{
    /// <summary>
    /// The activity definition IDs to delete.
    /// </summary>
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

/// <summary>
/// Represents a bulk-delete response.
/// </summary>
public class Response
{
    /// <summary>
    /// The number of records that were actually deleted.
    /// </summary>
    public int Deleted { get; set; }
}