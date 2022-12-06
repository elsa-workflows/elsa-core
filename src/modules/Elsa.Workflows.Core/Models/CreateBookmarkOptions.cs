using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public record CreateBookmarkOptions(object? Payload = default, ExecuteActivityDelegate? Callback = default, string? ActivityTypeName = default);