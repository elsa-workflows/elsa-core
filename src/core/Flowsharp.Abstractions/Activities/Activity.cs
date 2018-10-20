namespace Flowsharp.Activities
{
    public abstract class Activity : IActivity
    {
        public virtual string Name => GetType().Name;
    }
}
