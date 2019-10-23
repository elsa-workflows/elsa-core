using System.Collections;
using System.Collections.Generic;
using Elsa.WorkflowDesigner.Models;

namespace Elsa.Dashboard.Options
{
    public class ActivityDefinitionList : IEnumerable<ActivityDefinitionModel>
    {
        public ActivityDefinitionList()
        {
            Items = new Dictionary<string, ActivityDefinitionModel>();
        }

        private IDictionary<string, ActivityDefinitionModel> Items { get; }
        
        public ActivityDefinitionList Add(ActivityDefinitionModel item)
        {
            Items[item.Type] = item;
            return this;
        }

        public IEnumerator<ActivityDefinitionModel> GetEnumerator() => Items.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}