using Elsa.Extensions;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Contracts;
using Humanizer;

namespace Elsa.JavaScript.TypeDefinitions.Providers;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
internal class ActivityOutputFunctionsDefinitionProvider : FunctionDefinitionProvider
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IActivityRegistryLookupService _activityRegistryLookup;
    private readonly IIdentityGraphService _identityGraphService;

    public ActivityOutputFunctionsDefinitionProvider(IActivityVisitor activityVisitor, IActivityRegistryLookupService activityRegistryLookup, IIdentityGraphService identityGraphService)
    {
        _activityVisitor = activityVisitor;
        _activityRegistryLookup = activityRegistryLookup;
        _identityGraphService = identityGraphService;
    }

    protected override async ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        // Output getters.
        var workflow = context.Workflow;
        var nodes = (await _activityVisitor.VisitAsync(workflow.Root, context.CancellationToken)).Flatten().Distinct().ToList();
        
        // Ensure identities.
        await _identityGraphService.AssignIdentitiesAsync(nodes);
        
        var activitiesWithOutputs = nodes.GetActivitiesWithOutputs(_activityRegistryLookup);
        var definitions = new List<FunctionDefinition>();

        await foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
        {
            definitions.AddRange(from output in activityDescriptor.Outputs
                select output.Name.Pascalize()
                into outputPascalName
                let activityIdPascalName = activity.Id.Pascalize()
                select CreateFunctionDefinition(builder => builder.Name($"get{outputPascalName}From{activityIdPascalName}").ReturnType("any")));
        }

        return definitions;
    }
}