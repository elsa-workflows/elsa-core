using Elsa.Extensions;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Contracts;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.JavaScript.TypeDefinitions.Providers;

/// Produces <see cref="FunctionDefinition"/>s for common functions.
[UsedImplicitly]
internal class ActivityOutputFunctionsDefinitionProvider(IActivityRegistryLookupService activityRegistryLookup) : FunctionDefinitionProvider
{
    protected override async ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var nodes = context.WorkflowGraph.Nodes;
        var activitiesWithOutputs = nodes.GetActivitiesWithOutputs(activityRegistryLookup).Where(x => x.activity.Name != null);
        var definitions = new List<FunctionDefinition>();

        await foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
        {
            definitions.AddRange(from output in activityDescriptor.Outputs
                select output.Name.Pascalize()
                into outputPascalName
                let activityNamePascalName = activity.Name.Pascalize()
                select CreateFunctionDefinition(builder => builder.Name($"get{outputPascalName}From{activityNamePascalName}").ReturnType("any")));
        }

        return definitions;
    }
}