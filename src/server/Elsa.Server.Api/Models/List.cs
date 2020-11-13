using System.Collections.Generic;
using System.Linq;

namespace Elsa.Server.Api.Models
{
    public class List<T>
    {
        public List(IEnumerable<T> items)
        {
            Items = items.ToList();
        }
        
        public ICollection<T> Items { get; }
    }
}