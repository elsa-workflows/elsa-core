using Elsa.ActivityDefinitions.Activities;
using Elsa.ActivityDefinitions.Entities;
using Elsa.Workflows.Core.Services;

namespace Elsa.ActivityDefinitions.Services;

/// <summary>
/// Constructs an <see cref="IActivity"/> from the specified <see cref="ActivityDefinition"/>
/// </summary>
public interface IActivityDefinitionMaterializer
{
    Task<IActivity> MaterializeAsync(ActivityDefinitionActivity activity, CancellationToken cancellationToken = default);
}