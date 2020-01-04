using Elsa.Models;
using Elsa.Services.Models;
using ProcessInstance = Elsa.Models.ProcessInstance;

namespace Elsa.Services
{
    using ProcessInstance = Elsa.Services.Models.ProcessInstance;
    
    public interface IWorkflowFactory
    {
        Workflow CreateProcess(ProcessDefinitionVersion definition);

        ProcessInstance CreateProcessInstance(
            Workflow workflow, 
            Variable? input = default,
            ProcessInstance? processInstance = default,
            string correlationId = default);
    }
}