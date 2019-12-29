using Elsa.Models;
using Elsa.Services.Models;
using ProcessInstance = Elsa.Models.ProcessInstance;

namespace Elsa.Services
{
    using ProcessInstance = Elsa.Services.Models.ProcessInstance;
    
    public interface IProcessFactory
    {
        Process CreateProcess(ProcessDefinitionVersion definition);

        ProcessInstance CreateProcessInstance(
            Process process, 
            Variable? input = default,
            ProcessInstance? processInstance = default,
            string correlationId = default);
    }
}