using Elsa.Workflows.Runtime.Messages;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Results;

/// <summary>
/// Represents the result of sending a stimulus the engine.
/// </summary>
[UsedImplicitly]
public record SendStimulusResult(ICollection<RunWorkflowInstanceResponse> WorkflowInstanceResponses);