namespace Flowsharp.Models
{
    public abstract class Activity : IActivity
    {
        public int Id { get; set; }
        public virtual string Name => GetType().Name;
        public ActivityMetadata Metadata { get; set; } = new ActivityMetadata();
    }
}
