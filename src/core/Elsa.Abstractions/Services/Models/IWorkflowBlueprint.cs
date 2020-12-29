﻿using Elsa.Models;

namespace Elsa.Services.Models
{
    public interface IWorkflowBlueprint : ICompositeActivityBlueprint
    {
        int Version { get; }
        string? TenantId { get; }
        bool IsSingleton { get; }
        bool IsEnabled { get; }
        bool IsPublished { get; }
        bool IsLatest { get; }
        
        /// <summary>
        /// An initial set of variables available to workflow instances.
        /// </summary>
        Variables Variables { get; }
        
        /// <summary>
        /// An optional context type around which this workflow revolves. For example, a document, a leave request or a job application.
        /// </summary>
        WorkflowContextOptions? ContextOptions { get; set; }
        
        WorkflowPersistenceBehavior PersistenceBehavior { get; }
        bool DeleteCompletedInstances { get; }
        
        /// <summary>
        /// A dictionary to store application-specific properties for a given workflow. 
        /// </summary>
        Variables CustomAttributes { get; }
    }
}