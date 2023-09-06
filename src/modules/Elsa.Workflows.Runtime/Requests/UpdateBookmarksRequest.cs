using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Contracts;

public record UpdateBookmarksRequest(string InstanceId, Diff<Bookmark> Diff, string? CorrelationId);