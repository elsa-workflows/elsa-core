using System.Collections.Generic;
using System.Linq;

namespace Elsa.Server.Api.Models
{
    public class PagedList<T>
    {
        public PagedList(IEnumerable<T> items, int page, int pageSize, int totalCount)
        {
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
            Items = items.ToList();
        }
        
        public ICollection<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
    }
}