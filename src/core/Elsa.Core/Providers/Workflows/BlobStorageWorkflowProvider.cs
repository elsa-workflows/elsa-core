using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using LinqKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Storage.Net;
using Storage.Net.Blobs;

namespace Elsa.Providers.Workflows
{
    public class BlobStorageWorkflowProviderOptions
    {
        public Func<IBlobStorage> BlobStorageFactory { get; set; } = () => StorageFactory.Blobs.InMemory();
    }

    public class BlobStorageWorkflowProvider : WorkflowProvider
    {
        private readonly IBlobStorage _storage;
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;
        private readonly IContentSerializer _contentSerializer;
        private readonly ILogger _logger;

        public BlobStorageWorkflowProvider(
            IOptions<BlobStorageWorkflowProviderOptions> options,
            IWorkflowBlueprintMaterializer workflowBlueprintMaterializer,
            IContentSerializer contentSerializer,
            ILogger<BlobStorageWorkflowProvider> logger)
        {
            _storage = options.Value.BlobStorageFactory();
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
            _contentSerializer = contentSerializer;
            _logger = logger;
        }

        public override async IAsyncEnumerable<IWorkflowBlueprint> ListAsync(
            VersionOptions versionOptions,
            int? skip = default,
            int? take = default,
            string? tenantId = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var enumerable = ListInternalAsync(cancellationToken).OrderBy(x => x.Name).WithVersion(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                enumerable = enumerable.Where(x => x.TenantId == tenantId);

            if (skip != null)
                enumerable = enumerable.Skip(skip.Value);

            if (take != null)
                enumerable = enumerable.Take(take.Value);

            await foreach (var workflowBlueprint in enumerable.WithCancellation(cancellationToken))
                yield return workflowBlueprint;
        }

        public override async ValueTask<int> CountAsync(VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var enumerable = ListInternalAsync(cancellationToken).WithVersion(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                enumerable = enumerable.Where(x => x.TenantId == tenantId);

            return await enumerable.CountAsync(cancellationToken);
        }

        public override async ValueTask<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            Expression<Func<IWorkflowBlueprint, bool>> predicate = x => x.Id == definitionId && x.WithVersion(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                predicate = predicate.And(x => x.TenantId == tenantId);

            return await ListInternalAsync(cancellationToken).FirstOrDefaultAsync(predicate.Compile(), cancellationToken);
        }

        public override async ValueTask<IWorkflowBlueprint?> FindByDefinitionVersionIdAsync(string definitionVersionId, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            Expression<Func<IWorkflowBlueprint, bool>> predicate = x => x.VersionId == definitionVersionId;

            if (!string.IsNullOrWhiteSpace(tenantId))
                predicate = predicate.And(x => x.TenantId == tenantId);

            return await ListInternalAsync(cancellationToken).FirstOrDefaultAsync(predicate.Compile(), cancellationToken);
        }

        public override async ValueTask<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            Expression<Func<IWorkflowBlueprint, bool>> predicate = x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.WithVersion(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                predicate = predicate.And(x => x.TenantId == tenantId);

            return await ListInternalAsync(cancellationToken).FirstOrDefaultAsync(predicate.Compile(), cancellationToken);
        }

        public override async ValueTask<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            Expression<Func<IWorkflowBlueprint, bool>> predicate = x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase) && x.WithVersion(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                predicate = predicate.And(x => x.TenantId == tenantId);

            return await ListInternalAsync(cancellationToken).FirstOrDefaultAsync(predicate.Compile(), cancellationToken);
        }

        public override async ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionIds(IEnumerable<string> definitionIds, VersionOptions versionOptions, CancellationToken cancellationToken = default)
        {
            var idList = definitionIds.ToList();
            return await ListInternalAsync(cancellationToken).Where(x => idList.Contains(x.Id) && x.WithVersion(versionOptions)).ToListAsync(cancellationToken);
        }

        public override async ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionVersionIds(IEnumerable<string> definitionVersionIds, CancellationToken cancellationToken = default)
        {
            var idList = definitionVersionIds.ToList();
            return await ListInternalAsync(cancellationToken).Where(x => idList.Contains(x.VersionId)).ToListAsync(cancellationToken);
        }

        public override async ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
        {
            var nameList = names.ToList();
            return await ListInternalAsync(cancellationToken).Where(x => nameList.Contains(x.VersionId)).ToListAsync(cancellationToken);
        }

        public override async ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) => 
            await ListInternalAsync(cancellationToken)
                .Where(x => string.Equals(tag, x.Tag, StringComparison.OrdinalIgnoreCase) 
                            && x.WithVersion(versionOptions) && (x.TenantId == default || x.TenantId == tenantId))
                .ToListAsync(cancellationToken);

        private async IAsyncEnumerable<IWorkflowBlueprint> ListInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var blobs = await _storage.ListFilesAsync(new ListOptions(), cancellationToken);

            foreach (var blob in blobs)
            {
                var json = await _storage.ReadTextAsync(blob.FullPath, Encoding.UTF8, cancellationToken);
                var model = _contentSerializer.Deserialize<WorkflowDefinition>(json);
                var blueprint = await TryMaterializeBlueprintAsync(model, cancellationToken);

                if (blueprint != null)
                    yield return blueprint;
            }
        }

        private async Task<IWorkflowBlueprint?> TryMaterializeBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            try
            {
                return await _workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to materialize workflow definition {WorkflowDefinitionId} with version {WorkflowDefinitionVersion}", workflowDefinition.DefinitionId, workflowDefinition.Version);
            }

            return null;
        }
    }
}