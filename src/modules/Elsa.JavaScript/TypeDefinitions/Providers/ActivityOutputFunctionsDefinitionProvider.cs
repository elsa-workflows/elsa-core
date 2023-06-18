using Elsa.Extensions;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Core.Contracts;
using Humanizer;

namespace Elsa.JavaScript.TypeDefinitions.Providers;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
internal class ActivityOutputFunctionsDefinitionProvider : FunctionDefinitionProvider
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IIdentityGraphService _identityGraphService;

    public ActivityOutputFunctionsDefinitionProvider(IActivityVisitor activityVisitor, IActivityRegistry activityRegistry, IIdentityGraphService identityGraphService)
    {
        _activityVisitor = activityVisitor;
        _activityRegistry = activityRegistry;
        _identityGraphService = identityGraphService;
    }

    protected override async ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        // Output getters.
        var nodes = (await _activityVisitor.VisitAsync(context.Workflow.Root, context.CancellationToken)).Flatten().Distinct().ToList();
        
        // Ensure identities.
        _identityGraphService.AssignIdentities(nodes);
        
        var activitiesWithOutputs = nodes.GetActivitiesWithOutputs(_activityRegistry);
        var definitions = new List<FunctionDefinition>();

        foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
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