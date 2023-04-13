using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public record BookmarkOptions(object? Payload = default, ExecuteActivityDelegate? Callback = default, string? BookmarkName = default, bool AutoBurn = true);