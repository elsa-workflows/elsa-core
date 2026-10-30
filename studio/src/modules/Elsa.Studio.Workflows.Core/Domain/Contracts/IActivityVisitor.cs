using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Represents a visitor that can visit an activity.
/// </summary>
public interface IActivityVisitor
{
    /// <summary>
    /// Visits the specified activity and returns a graph of activities.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ActivityNode> VisitAsync(JsonObject activity, CancellationToken cancellationToken = default);
}