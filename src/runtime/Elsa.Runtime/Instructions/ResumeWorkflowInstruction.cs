using Elsa.Persistence.Entities;
using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Instructions;

public record ResumeWorkflowInstruction(WorkflowBookmark WorkflowBookmark) : IWorkflowInstruction;