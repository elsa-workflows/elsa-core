using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Web.Management.ViewModels
{
    public class WorkflowInstancesViewModel
    {
        public Workflow Definition { get; set; }
        public ICollection<Workflow> Instances { get; set; }
    }
}