using System.Collections.Generic;
using System.Linq;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowDefinitionListViewModel
    {
        public IList<IGrouping<string, WorkflowDefinitionListItemModel>> WorkflowDefinitions { get; set; }
    }
}