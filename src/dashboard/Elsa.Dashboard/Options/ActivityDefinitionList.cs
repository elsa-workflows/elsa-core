using System.Collections;
using System.Collections.Generic;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Options
{
    public class ActivityDefinitionList : IEnumerable<ActivityDefinition>
    {
        public ActivityDefinitionList()
        {
            Items = new List<ActivityDefinition>();
        }

        public List<ActivityDefinition> Items { get; }
        
        public ActivityDefinitionList Add(ActivityDefinition item)
        {
            Items.Add(item);
            return this;
        }

        public IEnumerator<ActivityDefinition> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}