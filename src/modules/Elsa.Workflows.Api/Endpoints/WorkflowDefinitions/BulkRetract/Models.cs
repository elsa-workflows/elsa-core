namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkRetract;

internal class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

internal class Response
{
    public Response(ICollection<string> retracted, ICollection<string> alreadyRetracted, ICollection<string> notFound, ICollection<string> skipped)
    {
        Retracted = retracted;
        AlreadyRetracted = alreadyRetracted;
        NotPublished = alreadyRetracted;
        NotFound = notFound;
        Skipped = skipped;
    }

    public ICollection<string> Retracted { get; }
    public ICollection<string> AlreadyRetracted { get; }
    public ICollection<string> Skipped { get; }

    [Obsolete("Use AlreadyRetracted instead")]
    public ICollection<string> NotPublished { get; }

    public ICollection<string> NotFound { get; }
}