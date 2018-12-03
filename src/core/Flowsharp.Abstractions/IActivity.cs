using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivity
    {
        string Id { get; set; }
        string Name { get; }
        ActivityMetadata Metadata { get; set; }
    }
}
