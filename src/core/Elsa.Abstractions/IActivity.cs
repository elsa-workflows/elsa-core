using Elsa.Models;
using Newtonsoft.Json;

namespace Elsa
{
    public interface IActivity
    {
        string Id { get; set; }
        string TypeName { get; }
        string Alias { get; set; }
        ActivityMetadata Metadata { get; set; }
    }
}