namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents an entity that is linked, meaning it contains links used in the context of HATEOAS.
/// </summary>
public class LinkedEntity : VersionedEntity
{
    /// <summary>
    /// A list of links that with the possible actions used in the context of HATEOAS.
    /// </summary>
    public Link[]? Links { get; set; }
}
