using System.Collections.Generic;

namespace Elsa.Server.Api.Models
{
    public class PagedList<T> : List<T>
    {
        public PagedList(IEnumerable<T> items, int? page, int? pageSize, int totalCount) : base(items)
        {
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
        
        public int? Page { get; }
        public int? PageSize { get; }
        public int TotalCount { get; }
    }
}