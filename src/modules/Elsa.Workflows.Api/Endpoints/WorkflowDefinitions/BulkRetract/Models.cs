using System.Collections.Generic;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkRetract;

public class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

public class Response
{
    public Response(ICollection<string> retracted, ICollection<string> notPublished, ICollection<string> notFound)
    {
        Retracted = retracted;
        NotPublished = notPublished;
        NotFound = notFound;
    }

    public ICollection<string> Retracted { get; }
    public ICollection<string> NotPublished { get; }
    public ICollection<string> NotFound { get; }
}