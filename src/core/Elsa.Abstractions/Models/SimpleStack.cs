using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Models
{
    /// <summary>
    /// A custom stack implementation that gets serialized properly by NewtonsoftJson, respecting things like element order and typename handling for each element.
    /// </summary>
    public class SimpleStack<T> : List<T>
    {
        public SimpleStack()
        {
        }
        
        public SimpleStack(IEnumerable<T> collection) : base(collection)
        {
        }

        public void Push(T value) => Insert(0, value);
        
        public T Pop()
        {
            var item = this.ElementAt(0);
            RemoveAt(0);
            return item;
        }
        
        public T Peek() => this.ElementAt(0);

        public bool Contains(Func<T, bool> predicate) => this.Any(predicate);
    }
}