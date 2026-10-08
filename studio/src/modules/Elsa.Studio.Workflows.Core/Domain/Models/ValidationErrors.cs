using System.Net;
using System.Text.Json;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents a collection of validation errors.
/// </summary>
/// <param name="Errors">The individual error messages.</param>
/// <param name="StatusCode">
/// The HTTP status code the failure was raised with, when known. Callers that need to tell one kind of failure
/// from another by more than wording (e.g. a BPMN capability refusal, which is only ever a <c>422</c>) can gate on
/// this instead of matching the error text.
/// </param>
/// <param name="Code">
/// The machine-readable code the error body carried, when the server sent one (see e.g.
/// <see cref="Bpmn.BpmnErrorCodes"/> for the BPMN-specific codes). <see langword="null"/> for an older server that
/// does not send a code yet, or for a failure that never carries one; callers must treat both the same way — as an
/// unrecognized code — and fall back to <see cref="Errors"/>'s message.
/// </param>
/// <param name="Data">
/// The raw <c>data</c> member <see cref="Code"/> carries, when it carries any (today, only a BPMN import's
/// capability refusal, whose <c>capabilities</c> and <c>elementIds</c> are read by
/// <see cref="Bpmn.BpmnCapabilityRefusal.FromData"/>). <see langword="null"/> otherwise. Left as raw JSON here
/// because the shape is owned by whichever feature defines <see cref="Code"/>, not by this shared model.
/// </param>
public record ValidationErrors(
    IReadOnlyCollection<ValidationError> Errors,
    HttpStatusCode? StatusCode = null,
    string? Code = null,
    JsonElement? Data = null);