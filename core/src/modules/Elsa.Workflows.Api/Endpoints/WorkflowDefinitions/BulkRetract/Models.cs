namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkRetract;

internal class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

internal class Response(ICollection<string> retracted, ICollection<string> alreadyRetracted, ICollection<string> notFound, ICollection<string> skipped)
{
    public ICollection<string> Retracted { get; } = retracted;
    public ICollection<string> AlreadyRetracted { get; } = alreadyRetracted;
    public ICollection<string> Skipped { get; } = skipped;

    [Obsolete("Use AlreadyRetracted instead")]
    public ICollection<string> NotPublished { get; } = alreadyRetracted;

    public ICollection<string> NotFound { get; } = notFound;
}