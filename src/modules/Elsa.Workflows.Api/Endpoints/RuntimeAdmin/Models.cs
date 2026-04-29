using Elsa.Workflows.Runtime;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin;

/// <summary>Body for the pause endpoint. The reason is a free-form human-readable string captured on the audit event.</summary>
public class PauseRequest
{
    public string? Reason { get; set; }
}

/// <summary>Body for the resume endpoint. No fields today — kept as a class so the endpoint signature is uniform.</summary>
public class ResumeRequest
{
}

/// <summary>Body for the operator-force drain endpoint.</summary>
public class ForceDrainRequest
{
    public string? Reason { get; set; }
}

/// <summary>Composite quiescence state surfaced by status / pause / resume responses.</summary>
public class QuiescenceStateDto
{
    public string Reason { get; set; } = null!;
    public bool IsAcceptingNewWork { get; set; }
    public DateTimeOffset? PausedAt { get; set; }
    public DateTimeOffset? DrainStartedAt { get; set; }
    public string? PauseReasonText { get; set; }
    public string? PauseRequestedBy { get; set; }
    public string GenerationId { get; set; } = null!;
}

/// <summary>Per-ingress-source state surfaced by status / pause / resume responses.</summary>
public class IngressSourceStateDto
{
    public string Name { get; set; } = null!;
    public string State { get; set; } = null!;
    public string? LastError { get; set; }
    public DateTimeOffset? LastTransitionAt { get; set; }
}

/// <summary>Status / pause / resume response. Idempotent endpoints return the post-request state.</summary>
public class StatusResponse
{
    public QuiescenceStateDto State { get; set; } = null!;
    public List<IngressSourceStateDto> Sources { get; set; } = new();
    public int ActiveExecutionCycleCount { get; set; }
}

/// <summary>Force-drain outcome surfaced by the force endpoint.</summary>
public class DrainOutcomeDto
{
    public string OverallResult { get; set; } = null!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public TimeSpan PausePhaseDuration { get; set; }
    public TimeSpan WaitPhaseDuration { get; set; }
    public List<IngressSourceStateDto> Sources { get; set; } = new();
    public int ExecutionCyclesForceCancelledCount { get; set; }
    public List<string> ForceCancelledInstanceIds { get; set; } = new();
}

/// <summary>Force-drain endpoint response.</summary>
public class ForceDrainResponse
{
    public DrainOutcomeDto Outcome { get; set; } = null!;
}

/// <summary>Returned by resume/force endpoints when a drain is already in progress.</summary>
public class ConflictResponse
{
    public string Code { get; set; } = null!;
    public StatusResponse State { get; set; } = null!;
}

internal static class StatusResponseFactory
{
    public static QuiescenceStateDto MapState(QuiescenceState state) => new()
    {
        Reason = state.Reason.ToString(),
        IsAcceptingNewWork = state.IsAcceptingNewWork,
        PausedAt = state.PausedAt,
        DrainStartedAt = state.DrainStartedAt,
        PauseReasonText = state.PauseReasonText,
        PauseRequestedBy = state.PauseRequestedBy,
        GenerationId = state.GenerationId,
    };

    public static IngressSourceStateDto MapSource(IngressSourceSnapshot s) => new()
    {
        Name = s.Name,
        State = s.State.ToString(),
        LastError = s.LastError?.Message,
        LastTransitionAt = s.LastTransitionAt,
    };

    public static StatusResponse Build(RuntimeAdminStatus status) => new()
    {
        State = MapState(status.State),
        Sources = status.Sources.Select(MapSource).ToList(),
        ActiveExecutionCycleCount = status.ActiveExecutionCycleCount,
    };
}
