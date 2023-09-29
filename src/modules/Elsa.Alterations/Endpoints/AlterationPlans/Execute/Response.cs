using Elsa.Alterations.Core.Results;

namespace Elsa.Alterations.Endpoints.AlterationPlans.Execute;

/// <summary>
/// The response from the <see cref="Execute"/> endpoint.
/// </summary>
/// <param name="Result">The result of the alteration plan execution.</param>
public record Response(AlterationPlanExecutionResult Result);