namespace Elsa.Labels.Endpoints.WorkflowDefinitionLabels.Update;

public class Request
{
    public string Id { get; set; } = default!;
    public ICollection<string> LabelIds { get; set; } = default!;
}

public class Response
{
    public Response(ICollection<string> labelIds)
    {
        LabelIds = labelIds;
    }
    
    public ICollection<string> LabelIds { get; }
}