namespace Elsa.Services.Models
{
    public struct WorkflowOutputReference
    {
        public WorkflowOutputReference(string? providerName, string activityId)
        {
            ProviderName = providerName;
            ActivityId = activityId;
        }
        
        public string? ProviderName { get; }
        public string ActivityId { get; }
    }
}