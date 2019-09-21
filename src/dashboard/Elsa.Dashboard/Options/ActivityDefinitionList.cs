using System.Collections;
using System.Collections.Generic;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Options
{
    public class ActivityDefinitionList : IEnumerable<ActivityDefinition>
    {
        public ActivityDefinitionList()
        {
            Items = new Dictionary<string, ActivityDefinition>();
        }

        private IDictionary<string, ActivityDefinition> Items { get; }
        
        public ActivityDefinitionList Add(ActivityDefinition item)
        {
            Items[item.Type] = item;
            return this;
        }

        public IEnumerator<ActivityDefinition> GetEnumerator() => Items.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}