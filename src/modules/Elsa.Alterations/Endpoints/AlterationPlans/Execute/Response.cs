using Elsa.Alterations.Core.Models;

namespace Elsa.Alterations.Endpoints.AlterationPlans.Execute;

/// <summary>
/// The response from the <see cref="Execute"/> endpoint.
/// </summary>
public record Response(bool HasSucceeded, ICollection<AlterationLogEntry> LogEntries);