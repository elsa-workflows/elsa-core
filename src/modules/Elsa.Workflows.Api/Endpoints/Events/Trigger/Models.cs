using System.Collections.Generic;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

public class Request
{
    public string EventName { get; set; }
}

public class Response
{
    public Response(ICollection<ExecuteWorkflowInstructionResult> items)
    {
        Items = items;
    }

    public ICollection<ExecuteWorkflowInstructionResult> Items { get; set; }
}