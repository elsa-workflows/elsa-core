using Elsa.Extensions;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Options;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Contracts;
using Humanizer;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Providers;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
internal class ActivityOutputFunctionsDefinitionProvider(
    IActivityVisitor activityVisitor, 
    IActivityRegistryLookupService activityRegistryLookup, 
    IIdentityGraphService identityGraphService, 
    IOptions<JintOptions> options) : FunctionDefinitionProvider
{
    protected override async ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        if(options.Value.DisableWrappers)
            return [];
        
        // Output getters.
        var workflow = context.Workflow;
        var nodes = (await activityVisitor.VisitAsync(workflow.Root, context.CancellationToken)).Flatten().Distinct().ToList();
        
        // Ensure identities.
        await identityGraphService.AssignIdentitiesAsync(nodes);
        
        var activitiesWithOutputs = nodes.GetActivitiesWithOutputs(activityRegistryLookup).Where(x => x.activity.Name != null && x.activity.Name.IsValidVariableName());
        var definitions = new List<FunctionDefinition>();

        await foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
        {
            definitions.AddRange(from output in activityDescriptor.Outputs.Where(x => x.Name.IsValidVariableName())
                select output.Name.Pascalize()
                into outputPascalName
                let activityIdPascalName = activity.Id.Pascalize()
                select CreateFunctionDefinition(builder => builder.Name($"get{outputPascalName}From{activityIdPascalName}").ReturnType("any")));
        }

        return definitions;
    }
}