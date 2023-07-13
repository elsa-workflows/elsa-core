namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkRetract;

internal class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

internal class Response
{
    public Response(ICollection<string> retracted, ICollection<string> alreadyRetracted, ICollection<string> notFound)
    {
        Retracted = retracted;
        AlreadyRetracted = alreadyRetracted;
        NotPublished = alreadyRetracted;
        NotFound = notFound;
    }

    public ICollection<string> Retracted { get; }
    public ICollection<string> AlreadyRetracted { get; }

    [Obsolete("Use AlreadyRetracted instead")]
    public ICollection<string> NotPublished { get; }

    public ICollection<string> NotFound { get; }
}