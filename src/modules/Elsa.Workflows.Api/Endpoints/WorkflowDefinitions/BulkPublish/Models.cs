namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkPublish;

internal class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

internal class Response
{
    public Response(ICollection<string> published, ICollection<string> alreadyPublished, ICollection<string> notFound)
    {
        Published = published;
        AlreadyPublished = alreadyPublished;
        NotFound = notFound;
    }

    public ICollection<string> Published { get; }
    public ICollection<string> AlreadyPublished { get; }
    public ICollection<string> NotFound { get; }
}