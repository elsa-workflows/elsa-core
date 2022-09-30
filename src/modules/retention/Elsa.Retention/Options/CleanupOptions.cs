using System;
using Elsa.Retention.Contracts;
using Elsa.Retention.Filters;
using Elsa.Retention.Specifications;
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
        /// The maximum number of workflow instances to process at the same time.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// An action that configures the retention filter pipeline. Can be replaced with your own action to configure a custom pipeline with custom filters. 
        /// </summary>
        public Action<IRetentionFilterPipeline> ConfigurePipeline { get; set; } = pipeline => pipeline.AddFilter<CompletedWorkflowFilter>();


        /// <summary>
        /// An action that configures the specification filter pipeline (server side). Can be replaced with your own action to configure a custom pipeline with custom specifications. 
        /// </summary>
        public Action<IRetentionSpecificationFilter> ConfigureSpecificationFilter { get; set; } = specification => specification.AddAndSpecification(new CompletedWorkflowFilterSpecification());

    }
}