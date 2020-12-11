using System.Collections.Generic;

namespace Elsa.Server.Api.Models
{
    public class PagedList<T>
    {
        public PagedList(ICollection<T> items, int? page, int? pageSize, int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public ICollection<T> Items { get; set; }
        public int? Page { get; }
        public int? PageSize { get; }
        public int TotalCount { get; }
    }
}