using System.Dynamic;

namespace Flowsharp.Models
{
    public class ActivityMetadata
    {
        public ExpandoObject CustomFields { get; set; } = new ExpandoObject();
    }
}