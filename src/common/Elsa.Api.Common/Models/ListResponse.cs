namespace Elsa.Models;

/// <summary>
/// Represents a generic list response that offers a unified format for returning list of things from API endpoints.
/// </summary>
public class ListResponse<T>
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public ListResponse()
    {
    }
    
    /// <summary>
    /// Constructor accepting a list of items.
    /// </summary>
    public ListResponse(ICollection<T> items)
    {
        Items = items;
        Count = items.Count;
    }

    /// <summary>
    /// A list of items.
    /// </summary>
    public ICollection<T> Items { get; set; } = default!;
    
    /// <summary>
    /// The number of items in the list. 
    /// </summary>
    public long Count { get; set; }
}