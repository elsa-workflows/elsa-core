using Elsa.Models;
using Newtonsoft.Json;

namespace Elsa
{
    public interface IActivity
    {
        string Id { get; set; }
        string Name { get; }
        ActivityMetadata Metadata { get; set; }
        
        [JsonIgnore]
        ActivityDescriptor Descriptor { get; set; }
    }
}
