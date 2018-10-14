using System.Collections.Generic;

namespace Flowsharp.Models
{
    public class WorkflowExecutionScope
    {
        public WorkflowExecutionScope()
        {
            Variables = new Dictionary<string, object>();
        }
        
        public object ReturnValue { get; set; }
        public IDictionary<string, object> Variables { get; }

        public void SetVariable(string variableName, object value)
        {
            Variables[variableName] = value;
        }
        
        public T GetVariable<T>(string name)
        {
            return Variables.ContainsKey(name) ? (T)Variables[name] : default(T);
        }
    }
}