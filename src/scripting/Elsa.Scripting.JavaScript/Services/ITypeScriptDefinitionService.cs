using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface ITypeScriptDefinitionService
    {
        Task<string> GenerateTypeScriptDefinitionsAsync(WorkflowDefinition? workflowDefinition = default, IntellisenseContext? context = default, CancellationToken cancellationToken = default);
    }
}