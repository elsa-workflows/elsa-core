using System.Collections.Generic;

namespace Elsa.Client.Models
{
    public class List<T>
    {
        public ICollection<T> Items { get; set; } = new System.Collections.Generic.List<T>();
    }
}