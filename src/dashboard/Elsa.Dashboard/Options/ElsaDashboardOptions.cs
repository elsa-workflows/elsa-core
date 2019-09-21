using System.Collections.Generic;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Options
{
    public class ElsaDashboardOptions
    {
        public ElsaDashboardOptions()
        {
            Scripts = new List<string>();
            ActivityDefinitions = new List<ActivityDefinition>();
        }
        
        public IList<string> Scripts { get; set; }
        public IList<ActivityDefinition> ActivityDefinitions { get; set; }
    }
}