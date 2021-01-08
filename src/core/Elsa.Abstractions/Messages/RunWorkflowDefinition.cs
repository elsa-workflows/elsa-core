namespace Elsa.Messages
{
    public class RunWorkflowDefinition
    {
        public RunWorkflowDefinition(string workflowDefinitionId, string? tenantId, string? activityId = default, object? input = default, string? correlationId = default, string? contextId = default)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            TenantId = tenantId;
            ActivityId = activityId;
            Input = input;
            CorrelationId = correlationId;
            ContextId = contextId;
        }

        public string WorkflowDefinitionId { get; set; }
        public string? TenantId { get; set; }
        public string? ActivityId { get; set; }
        public object? Input { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextId { get; set; }
    }
}