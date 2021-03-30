﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowRegistryExtensions
    {
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            string? tenantId,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetAsync(typeof(T).Name, tenantId, VersionOptions.Latest, cancellationToken);

        public static Task<IWorkflowBlueprint?> GetWorkflowAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync<T>(default, cancellationToken);
        
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync(
            this IWorkflowRegistry workflowRegistry,
            string id,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync(id, default, versionOptions, cancellationToken);
        
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync(
            this IWorkflowRegistry workflowRegistry,
            string id,
            string? tenantId,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetAsync(id, tenantId, versionOptions, cancellationToken);
    }
}