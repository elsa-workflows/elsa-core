using System.Collections.Generic;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

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
}