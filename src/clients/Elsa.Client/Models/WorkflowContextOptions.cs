using System;

namespace Elsa.Client.Models
{
    public class WorkflowContextOptions
    {
        public Type ContextType { get; set; } = default!;
        public WorkflowContextFidelity ContextFidelity { get; set; }
    }
}