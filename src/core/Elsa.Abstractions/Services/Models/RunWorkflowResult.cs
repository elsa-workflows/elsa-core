using System;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public record RunWorkflowResult(WorkflowInstance? WorkflowInstance, string? ActivityId, Exception? Exception, bool Executed);
}