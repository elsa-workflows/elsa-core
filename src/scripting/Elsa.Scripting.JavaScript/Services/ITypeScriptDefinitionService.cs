using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface ITypeScriptDefinitionService
    {
        string GenerateTypeScriptDefinition(WorkflowDefinition? workflowDefinition = default);
    }
}