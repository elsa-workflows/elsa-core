namespace Elsa.Triggers
{
    public interface IWorkflowTriggerHasher
    {
        string Hash(ITrigger trigger);
    }
}