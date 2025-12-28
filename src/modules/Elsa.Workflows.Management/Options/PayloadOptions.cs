namespace Elsa.Workflows.Management.Options;

public sealed class PayloadOptions
{
    public PayloadPersistenceOption? WorkflowInstancesPersistence { get; set; }

    public PayloadPersistenceOption? WorkflowDefinitionsPersistence { get; set; }
}
