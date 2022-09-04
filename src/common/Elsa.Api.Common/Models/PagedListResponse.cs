using Elsa.Common.Models;

namespace Elsa.Models;

public class PagedListResponse<T>
{
    public PagedListResponse()
    {
    }
    
    public PagedListResponse(Page<T> page)
    {
        Items = page.Items;
        TotalCount = page.TotalCount;
    }

    public ICollection<T> Items { get; set; } = default!;
    public long TotalCount { get; set; }
}