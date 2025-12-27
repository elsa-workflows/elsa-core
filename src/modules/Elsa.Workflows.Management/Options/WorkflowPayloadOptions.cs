namespace Elsa.Workflows.Management.Options;

public sealed class WorkflowPayloadOptions
{
    public KeyValuePair<string, WorkflowPayloadPersistenceMode>? WorkflowInstancesPersistence { get; set; }

    public KeyValuePair<string, WorkflowPayloadPersistenceMode>? WorkflowDefinitionsPersistence { get; set; }
}
