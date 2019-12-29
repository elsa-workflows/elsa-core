using Elsa.Models;

namespace Elsa.Services.Models
{
    public class Process
    {
        public Process()
        {
        }

        public Process(
            string definitionId = default,
            int version = 1,
            bool isSingleton = false,
            bool isDisabled = false,
            string? name = default,
            string? description = default,
            bool isLatest = false,
            bool isPublished = false,
            IActivity? start = default)
        {
            DefinitionId = definitionId;
            Version = version;
            IsSingleton = isSingleton;
            IsDisabled = isDisabled;
            IsLatest = isLatest;
            IsPublished = isPublished;
            Name = name;
            Description = description;
            Start = start;
        }

        public string? DefinitionId { get; }
        public int Version { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsDisabled { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public IActivity Start { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public ProcessPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
    }
}