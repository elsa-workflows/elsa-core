using System.Collections.Generic;

namespace Flowsharp.Models
{
    public class Variables : Dictionary<string, object>
    {        
        public static readonly Variables Empty = new Variables();

        public object GetVariable(string name)
        {
            return ContainsKey(name) ? this[name] : null; 
        }
        
        public T GetVariable<T>(string name)
        {
            return ContainsKey(name) ? (T)this[name] : default(T); 
        }
    }
}