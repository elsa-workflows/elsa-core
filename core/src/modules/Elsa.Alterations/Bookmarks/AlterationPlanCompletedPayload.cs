using Elsa.Alterations.Activities;

namespace Elsa.Alterations.Bookmarks;

/// <summary>
/// Represents bookmark payload for the <see cref="AlterationPlanCompleted"/> trigger.
/// </summary>
/// <param name="PlanId">The ID of the alteration plan.</param>
public record AlterationPlanCompletedPayload(string PlanId);