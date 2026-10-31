namespace Elsa.Alterations.MassTransit.Messages;

/// <summary>
/// Represents a request to run an alteration job.
/// </summary>
/// <param name="JobId">The job ID.</param>
public record RunAlterationJob(string JobId);