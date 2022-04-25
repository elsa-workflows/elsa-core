using Elsa.Models;

namespace Elsa.Runtime.Models;

public record ExecuteWorkflowInstructionResult(Workflow Workflow, InvokeWorkflowResult InvokeWorkflowResult);