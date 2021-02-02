namespace Elsa.Client.Models
{
    public record RetryWorkflowRequest(bool RunImmediately)
    {
        /// <summary>
        /// Set to true to run the revived workflow immediately, set to false to enqueue the revived workflow for execution.
        /// </summary>
        public bool RunImmediately { get; set; } = RunImmediately;
    }
}