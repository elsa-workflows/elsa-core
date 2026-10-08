namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Options for dynamic outcomes.
/// </summary>
/// <param name="FixedOutcomes">A list of fixed outcomes that will be appended to the final list of outcomes.</param>
public record DynamicOutcomesOptions(ICollection<string>? FixedOutcomes);