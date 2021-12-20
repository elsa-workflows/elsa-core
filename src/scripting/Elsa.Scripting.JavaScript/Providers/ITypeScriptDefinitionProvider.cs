using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Models;

namespace Elsa.Scripting.JavaScript.Providers
{
    public interface ITypeScriptDefinitionProvider
    {
        Task<StringBuilder> GenerateTypeScriptDefinitionsAsync(StringBuilder builder, ICollection<string> declaredTypes, WorkflowDefinition? workflowDefinition = default, IntellisenseContext? context = default, CancellationToken cancellationToken = default);
    }
}
