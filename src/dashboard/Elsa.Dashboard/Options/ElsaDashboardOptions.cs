using System.Collections.Generic;

namespace Elsa.Dashboard.Options
{
    public class ElsaDashboardOptions
    {
        public ElsaDashboardOptions()
        {
            Scripts = new List<string>();
            ActivityDefinitions = new ActivityDefinitionList();
        }
        
        public IList<string> Scripts { get; set; }
        public ActivityDefinitionList ActivityDefinitions { get; set; }
    }
}