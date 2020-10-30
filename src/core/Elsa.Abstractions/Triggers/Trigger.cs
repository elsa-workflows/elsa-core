namespace Elsa.Triggers
{
    public abstract class Trigger : ITrigger
    {
        public virtual bool IsOneOff { get; }
    }
}