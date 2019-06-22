using System.Collections.Generic;

namespace Elsa.Models
{
    public class Variables : Dictionary<string, object>
    {        
        public static readonly Variables Empty = new Variables();

        public Variables()
        {
        }

        public Variables(Variables other)
        {
            foreach (var variable in other)
            {
                this[variable.Key] = variable.Value;
            }
        }
        
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