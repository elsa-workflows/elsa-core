using System.Collections.Generic;
using System.Linq;

namespace Elsa.Server.Api.Models
{
    public record ListModel<T>
    {
        public ListModel(IEnumerable<T> items) => Items = items.ToArray();
        public T[] Items { get; set; }
    }
}