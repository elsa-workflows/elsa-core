using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Models;
using Elsa.Scripting.JavaScript.Providers;

namespace Elsa.Scripting.JavaScript.Services
{
    public class TypeScriptDefinitionService : ITypeScriptDefinitionService
    {
        private readonly IEnumerable<ITypeScriptDefinitionProvider> _typeScriptDefinitionProviders;

        public TypeScriptDefinitionService(IEnumerable<ITypeScriptDefinitionProvider> typeScriptDefinitionProviders)
        {
            _typeScriptDefinitionProviders = typeScriptDefinitionProviders;
        }

        public async Task<string> GenerateTypeScriptDefinitionsAsync(WorkflowDefinition? workflowDefinition = default, IntellisenseContext? context = default, CancellationToken cancellationToken = default)
        {
            var providers = _typeScriptDefinitionProviders;
            var builder = new StringBuilder();
            var declaredTypes = new List<string>();

            foreach (var provider in providers)
            {
                builder = await provider.GenerateTypeScriptDefinitionsAsync(builder, declaredTypes, workflowDefinition, context, cancellationToken);
            }

            return builder.ToString();
        }
    }
}
