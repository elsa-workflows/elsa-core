namespace Elsa.Triggers
{
    public interface ITrigger
    {
        /// <summary>
        /// A value indicating whether this trigger should execute one time only.
        /// When true, the trigger is removed from the list of triggers once it was triggered.
        /// Otherwise, the trigger remains active.
        /// </summary>
        bool IsOneOff { get; }
    }
}