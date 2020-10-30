namespace Elsa.Triggers
{
    /// <summary>
    /// A non-trigger.
    /// </summary>
    public class NullTrigger : Trigger
    {
        public static readonly NullTrigger Instance = new NullTrigger();
    }
}