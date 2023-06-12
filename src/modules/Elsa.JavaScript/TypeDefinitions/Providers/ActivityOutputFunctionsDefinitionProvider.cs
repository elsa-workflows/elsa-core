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

    public ActivityOutputFunctionsDefinitionProvider(IActivityVisitor activityVisitor, IActivityRegistry activityRegistry)
    {
        _activityVisitor = activityVisitor;
        _activityRegistry = activityRegistry;
    }

    protected override async ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        // Output getters.
        var nodes = (await _activityVisitor.VisitAsync(context.Workflow.Root, context.CancellationToken)).Flatten().Distinct().ToList();
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