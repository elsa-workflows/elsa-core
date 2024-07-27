using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

public record GetPathSegmentsResponse(ActivityNode ChildNode, ActivityNode Container, ICollection<ActivityPathSegment> PathSegments);