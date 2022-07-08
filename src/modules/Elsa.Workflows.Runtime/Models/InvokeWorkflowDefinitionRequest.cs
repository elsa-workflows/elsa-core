using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Models;

namespace Elsa.Workflows.Runtime.Models;

public record InvokeWorkflowDefinitionRequest(string DefinitionId, VersionOptions VersionOptions, IDictionary<string, object>? Input = default, string? CorrelationId = default);