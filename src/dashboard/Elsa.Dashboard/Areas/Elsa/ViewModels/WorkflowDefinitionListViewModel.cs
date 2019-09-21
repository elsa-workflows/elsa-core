using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowDefinitionListViewModel
    {
        public IList<IGrouping<string, WorkflowDefinitionListItemModel>> WorkflowDefinitions { get; set; }
    }
}