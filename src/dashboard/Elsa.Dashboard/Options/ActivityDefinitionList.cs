using System.Collections;
using System.Collections.Generic;
using Elsa.Metadata;

namespace Elsa.Dashboard.Options
{
    public class ActivityDefinitionList : IEnumerable<ActivityDescriptor>
    {
        public ActivityDefinitionList()
        {
            Items = new Dictionary<string, ActivityDescriptor>();
        }

        private IDictionary<string, ActivityDescriptor> Items { get; }
        
        public ActivityDefinitionList Add(ActivityDescriptor item)
        {
            Items[item.Type] = item;
            return this;
        }

        public IEnumerator<ActivityDescriptor> GetEnumerator() => Items.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}