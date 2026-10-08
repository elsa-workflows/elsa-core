using Elsa.Alterations.Core.Entities;

namespace Elsa.Alterations.Endpoints.Alterations.Get;

/// <summary>
/// Represents a response to a request to get an alteration plan. 
/// </summary>
/// <param name="Plan">The alteration plan.</param>
/// <param name="Jobs">The jobs created for the plan.</param>
public record Response(AlterationPlan Plan, ICollection<AlterationJob> Jobs);