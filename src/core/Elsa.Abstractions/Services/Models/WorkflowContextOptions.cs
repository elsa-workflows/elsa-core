using System;

namespace Elsa.Services.Models
{
    public class WorkflowContextOptions
    {
        public Type ContextType { get; set; } = default!;
        public WorkflowContextFidelity ContextFidelity { get; set; }
    }
}