﻿using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Requests
{
    /// <summary>
    /// Represents a request to save a workflow definition.
    /// </summary>
    public class SaveWorkflowDefinitionRequest
    {
        /// <summary>
        /// The workflow definition to save.
        /// </summary>
        public WorkflowDefinitionModel Model { get; set; } = default!;

        /// <summary>
        /// Whether the workflow definition should be published.
        /// </summary>
        public bool? Publish { get; set; }
    }
}
