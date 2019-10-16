using System.Collections;
using System.Collections.Generic;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Options
{
    public class ActivityDefinitionList : IEnumerable<DesignerActivityDefinition>
    {
        public ActivityDefinitionList()
        {
            Items = new Dictionary<string, DesignerActivityDefinition>();
        }

        private IDictionary<string, DesignerActivityDefinition> Items { get; }
        
        public ActivityDefinitionList Add(DesignerActivityDefinition item)
        {
            Items[item.Type] = item;
            return this;
        }

        public IEnumerator<DesignerActivityDefinition> GetEnumerator() => Items.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}