using Elsa.Models;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionScope
    {
        public WorkflowExecutionScope(Variables? variables = default)
        {
            Variables = variables ?? new Variables();
        }
        
        public Variables Variables { get; }

        public void SetVariable(string variableName, object value) => Variables.SetVariable(variableName, value);
        public T GetVariable<T>(string name) => Variables.GetVariable<T>(name);
        public object GetVariable(string name) => Variables.GetVariable(name);
    }
}