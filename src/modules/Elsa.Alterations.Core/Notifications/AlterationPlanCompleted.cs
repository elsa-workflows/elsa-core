using Elsa.Alterations.Core.Entities;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.Core.Notifications;

/// <summary>
/// A notification that is published when an alteration plan is completed.
/// </summary>
public record AlterationPlanCompleted(AlterationPlan Plan) : INotification;