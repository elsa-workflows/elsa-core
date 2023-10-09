using Elsa.Alterations.Core.Entities;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.Core.Notifications;

/// <summary>
/// A notification that is published when an alteration plan is submitted.
/// </summary>
public record AlterationPlanSubmitted(AlterationPlan Plan) : INotification;