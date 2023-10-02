using Elsa.Alterations.Core.Models;

namespace Elsa.Alterations.Endpoints.AlterationPlans.Submit;

/// <summary>
/// A plan that contains a list of alterations to be applied to a set of workflow instances.
/// </summary>
public class Request
{
    /// <summary>
    /// The plan to execute.
    /// </summary>
    public NewAlterationPlan Plan { get; set; }
}