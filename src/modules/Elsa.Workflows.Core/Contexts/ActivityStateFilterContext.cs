using System.Text.Json;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public record ActivityStateFilterContext(ActivityExecutionContext ActivityExecutionContext, InputDescriptor InputDescriptor, JsonElement Value, CancellationToken CancellationToken);