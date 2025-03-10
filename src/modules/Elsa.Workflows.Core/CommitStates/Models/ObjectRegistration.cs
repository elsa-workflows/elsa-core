namespace Elsa.Workflows.CommitStates;

public class ObjectRegistration<T, TMeta>
{
    public T Strategy { get; set; } = default!;
    public TMeta Metadata { get; set; } = default!;
}