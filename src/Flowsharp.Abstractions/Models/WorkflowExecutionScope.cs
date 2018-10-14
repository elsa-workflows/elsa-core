using System.Collections.Generic;
using Newtonsoft.Json;

namespace Flowsharp.Models
{
    public class WorkflowExecutionScope
    {
        public WorkflowExecutionScope()
        {
            Variables = new Variables();
        }
        
        public object LastResult { get; set; }
        public Variables Variables { get; }

        public void SetVariable(string variableName, object value)
        {
            Variables[variableName] = value;
        }
        
        public T GetVariable<T>(string name)
        {
            return Variables.ContainsKey(name) ? (T)Variables[name] : default(T);
        }
    }

    [JsonDictionary(ItemTypeNameHandling = TypeNameHandling.None)]
    public class Variables : Dictionary<string, object>
    {
        
    }
}