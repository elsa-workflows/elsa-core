using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowInstanceListViewModel
    {
        public WorkflowDefinitionVersion WorkflowDefinition { get; set; }
        public string ReturnUrl { get; set; }
        public IList<WorkflowInstanceListItemModel> WorkflowInstances { get; set; }
    }
}