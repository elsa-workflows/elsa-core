using Elsa.Studio.UserTasks.Models;

namespace Elsa.Studio.UserTasks.Contracts;

/// <summary>
/// Optional host contribution for selecting identity-neutral participant references.
/// </summary>
public interface IUserTaskParticipantPicker
{
    ValueTask<ParticipantPickerResult?> PickAsync(ParticipantPickerContext context, CancellationToken cancellationToken = default);
}

public sealed record ParticipantPickerContext(
    IReadOnlyCollection<UserTaskParticipantSummary> CurrentValues,
    IReadOnlyCollection<string> AllowedKinds,
    string? TaskKey,
    string? TenantId,
    bool Multiple);

public sealed record ParticipantPickerResult(IReadOnlyCollection<UserTaskParticipantSummary> Participants);
