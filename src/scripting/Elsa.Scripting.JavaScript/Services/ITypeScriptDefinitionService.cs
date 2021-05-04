using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface ITypeScriptDefinitionService
    {
        Task<string> GenerateTypeScriptDefinitionsAsync(WorkflowDefinition? workflowDefinition = default, string? context = default, CancellationToken cancellationToken = default);
    }
}