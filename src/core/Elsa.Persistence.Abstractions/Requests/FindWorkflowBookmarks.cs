using Elsa.Helpers;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Services;

namespace Elsa.Persistence.Requests;

public record FindWorkflowBookmarks : IRequest<IEnumerable<WorkflowBookmark>>
{
    public FindWorkflowBookmarks(string workflowInstanceId)
    {
        WorkflowInstanceId = workflowInstanceId;
    }

    public FindWorkflowBookmarks(string name, string? hash)
    {
        Name = name;
        Hash = hash;
    }

    public string? WorkflowInstanceId { get; }
    public string? Name { get; }
    public string? Hash { get; }
    
    public static FindWorkflowBookmarks ForActivity<T>(string? hash = default) where T : IActivity => new(TypeNameHelper.GenerateTypeName<T>(), hash);
}