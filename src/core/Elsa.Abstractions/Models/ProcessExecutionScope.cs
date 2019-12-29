namespace Elsa.Models
{
    public class ProcessExecutionScope
    {
        public ProcessExecutionScope()
        {
            Variables = new Variables();
        }
        
        public Variables Variables { get; }

        public void SetVariable(string variableName, object value) => Variables.SetVariable(variableName, value);
        public T GetVariable<T>(string name) => Variables.GetVariable<T>(name);
        public object GetVariable(string name) => Variables.GetVariable(name);
    }
}