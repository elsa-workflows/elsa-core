using System.Collections.Generic;
using System.Linq;

namespace Elsa.Client.Models
{
    public class PagedList<T>
    {
        public ICollection<T> Items { get; set; } = new List<T>();
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}