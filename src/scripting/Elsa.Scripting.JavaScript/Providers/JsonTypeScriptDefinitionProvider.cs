using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Providers;

namespace Elsa.Scripting.JavaScript.Services
{
    public class JsonTypeScriptDefinitionProvider : ITypeScriptDefinitionProvider
    {
        public JsonTypeScriptDefinitionProvider() {}

        public async Task<StringBuilder> GenerateTypeScriptDefinitionsAsync(StringBuilder builder, ICollection<string> declaredTypes, WorkflowDefinition? workflowDefinition = default, string? context = default, CancellationToken cancellationToken = default)
        {
            if (workflowDefinition == null) 
                return builder;

            return builder;
        }
    }
}