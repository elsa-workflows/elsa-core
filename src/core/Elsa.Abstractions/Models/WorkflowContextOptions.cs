using System;
using Elsa.Services.Models;

namespace Elsa.Models
{
    public class WorkflowContextOptions
    {
        public Type ContextType { get; set; } = default!;
        public WorkflowContextFidelity ContextFidelity { get; set; }
    }
}