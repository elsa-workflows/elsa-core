using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivity
    {
        int Id { get; set; }
        string Name { get; }
        ActivityMetadata Metadata { get; set; }
    }
}
