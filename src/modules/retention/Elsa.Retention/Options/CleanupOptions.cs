using NodaTime;

namespace Elsa.Retention.Options
{
    public class CleanupOptions
    {
        /// <summary>
        /// Controls how often the database is checked for workflow instances and execution log records to remove. 
        /// </summary>
        public Duration SweepInterval { get; set; } = Duration.FromHours(4);

        /// <summary>
        /// The maximum age a workflow instance is allowed to exist before being removed.
        /// </summary>
        public Duration TimeToLive { get; set; }

        /// <summary>
        /// The maximum number of workflow instances to delete at the same time.
        /// </summary>
        public int PageSize { get; set; } = 100;
    }
}