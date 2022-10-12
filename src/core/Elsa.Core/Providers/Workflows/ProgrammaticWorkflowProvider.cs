using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Options;
using Elsa.Services.Models;

namespace Elsa.Providers.Workflows
{
    /// <summary>
    /// Provides programmatic workflows (.NET types).
    /// </summary>
    public class ProgrammaticWorkflowProvider : WorkflowProvider
    {
        private readonly IEnumerable<IWorkflow> _workflows;
        private readonly Func<IWorkflowBuilder> _workflowBuilder;

        public ProgrammaticWorkflowProvider(ElsaOptions elsaOptions, IServiceProvider serviceProvider, Func<IWorkflowBuilder> workflowBuilder)
        {
            _workflows = elsaOptions.WorkflowFactory.CreateServices(serviceProvider);
            _workflowBuilder = workflowBuilder;
        }

        public override IAsyncEnumerable<IWorkflowBlueprint> ListAsync(VersionOptions versionOptions, int? skip = default, int? take = default, string? tenantId = default, CancellationToken cancellationToken = default) =>
            List(versionOptions, skip, take, tenantId).OrderBy(x => x.Name).ToAsyncEnumerable();

        public override ValueTask<int> CountAsync(VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) =>
            new(List(versionOptions, tenantId: tenantId).Count());

        public override ValueTask<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = GetWorkflows().FirstOrDefault(x => x.Id == definitionId && x.WithVersion(versionOptions));
            return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
        }

        public override ValueTask<IWorkflowBlueprint?> FindByDefinitionVersionIdAsync(string definitionVersionId, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = GetWorkflows().FirstOrDefault(x => x.VersionId == definitionVersionId);
            return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
        }

        public override ValueTask<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = GetWorkflows().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.WithVersion(versionOptions));
            return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
        }

        public override ValueTask<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = GetWorkflows().FirstOrDefault(x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase) && x.WithVersion(versionOptions));
            return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
        }

        public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionIds(IEnumerable<string> definitionIds, VersionOptions versionOptions, CancellationToken cancellationToken = default)
        {
            var workflowBlueprints = GetWorkflows().Where(x => definitionIds.Contains(x.Id) && x.WithVersion(versionOptions)).ToList();
            return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
        }

        public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionVersionIds(IEnumerable<string> definitionVersionIds, CancellationToken cancellationToken = default)
        {
            var workflowBlueprints = GetWorkflows().Where(x => definitionVersionIds.Contains(x.VersionId)).ToList();
            return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
        }

        public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
        {
            var workflowBlueprints = GetWorkflows().Where(x => names.Contains(x.Name)).ToList();
            return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
        }

        public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprints = GetWorkflows().Where(x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase)).ToList();
            return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
        }

        private IEnumerable<IWorkflowBlueprint> List(VersionOptions versionOptions, int? skip = default, int? take = default, string? tenantId = default)
        {
            var enumerable = GetWorkflows().WithVersion(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                enumerable = enumerable.Where(x => x.TenantId == tenantId);

            if (skip != null)
                enumerable = enumerable.Skip(skip.Value);

            if (take != null)
                enumerable = enumerable.Take(take.Value);

            foreach (var workflowBlueprint in enumerable)
                yield return workflowBlueprint;
        }

        private IEnumerable<IWorkflowBlueprint> GetWorkflows() => from workflow in _workflows let builder = _workflowBuilder() select builder.Build(workflow);
    }
}