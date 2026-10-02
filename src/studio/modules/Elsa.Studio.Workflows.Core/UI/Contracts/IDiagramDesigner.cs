using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Represents a diagram editor.
/// </summary>
public interface IDiagramDesigner
{
    /// <summary>
    /// Loads the specified root activity int the designer.
    /// </summary>
    /// <param name="activity">The root activity to load.</param>
    /// <param name="activityStatsMap">A map of activity stats.</param>
    Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap);
    
    /// <summary>
    /// Updates the specified activity in the diagram. This is used to update the diagram when an activity is updated in the activity editor.
    /// </summary>
    /// <param name="id">The ID of the activity to update.</param>
    /// <param name="activity">The activity to update.</param>
    Task UpdateActivityAsync(string id, JsonObject activity);

    /// <summary>
    /// Updates the stats of the specified activity.
    /// </summary>
    /// <param name="id">The ID of the activity to update.</param>
    /// <param name="stats">The stats to update.</param>
    Task UpdateActivityStatsAsync(string id, ActivityStats stats);
    
    /// <summary>
    /// Selects the specified activity in the diagram.
    /// </summary>
    /// <param name="id">The ID of the activity to select.</param>
    Task SelectActivityAsync(string id);
    
    /// <summary>
    /// Reads the root activity from the diagram.
    /// </summary>
    Task<JsonObject> ReadRootActivityAsync();

    /// <summary>
    /// Display the designer.
    /// </summary>
    RenderFragment DisplayDesigner(DisplayContext context);
}