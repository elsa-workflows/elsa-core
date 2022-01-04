using Elsa.Models;

namespace Elsa.Runtime.Contracts;

public record ExecuteWorkflowInstructionResult(Workflow Workflow, ExecuteWorkflowResult ExecuteWorkflowResult);