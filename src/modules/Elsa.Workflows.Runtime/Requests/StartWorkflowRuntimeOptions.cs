using Elsa.Common.Models;

namespace Elsa.Workflows.Runtime.Contracts;

public record StartWorkflowRuntimeOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default, VersionOptions VersionOptions = default, string? TriggerActivityId = default, string? InstanceId = default);