namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkPublish;

internal class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

internal class Response(ICollection<string> published, ICollection<string> alreadyPublished, ICollection<string> notFound, ICollection<string> skipped, ICollection<string> updatedConsumers, ICollection<string> failed, IDictionary<string, ICollection<string>> warnings)
{
    public ICollection<string> Published { get; } = published;
    public ICollection<string> AlreadyPublished { get; } = alreadyPublished;
    public ICollection<string> NotFound { get; } = notFound;
    public ICollection<string> Skipped { get; } = skipped;
    public ICollection<string> UpdatedConsumers { get; } = updatedConsumers;
    public ICollection<string> Failed { get; } = failed;
    public IDictionary<string, ICollection<string>> Warnings { get; } = warnings;
}