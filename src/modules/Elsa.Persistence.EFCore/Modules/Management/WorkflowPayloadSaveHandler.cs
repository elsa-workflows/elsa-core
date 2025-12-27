using System.Diagnostics.CodeAnalysis;
using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Common.Entities;
using Elsa.Workflows;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Persistence.EFCore.Modules.Management;

public sealed class WorkflowPayloadSaveHandler(
    IWorkflowStateSerializer workflowStateSerializer,
    IWorkflowPayloadStoreManager payloadStoreManager,
    ICompressionCodecResolver compressionCodecResolver,
    IOptions<ManagementOptions> _options,
    IOptions<WorkflowPayloadOptions> payloadOptions
) 
    
: IEntitySavingHandler
{
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.SerializeAsync(WorkflowState, CancellationToken)")]
    public async ValueTask HandleAsync(ElsaDbContextBase dbContext, EntityEntry entry, CancellationToken cancellationToken = default)
    {
        if(entry.Entity is not WorkflowInstance workflowInstance)
        {
            return;
        }

        var data = workflowInstance.WorkflowState;
        var json = workflowStateSerializer.Serialize(data);
        
        var compressionAlgorithm = _options.Value.CompressionAlgorithm ?? nameof(None);
        var compressionCodec = compressionCodecResolver.Resolve(compressionAlgorithm);
        var compressedJson = await compressionCodec.CompressAsync(json, cancellationToken);

        dbContext.Entry(workflowInstance).Property("DataCompressionAlgorithm").CurrentValue = compressionAlgorithm;
        await SetPayloadData(dbContext, workflowInstance, compressedJson, cancellationToken);
    }

    private async ValueTask SetPayloadData(ElsaDbContextBase managementElsaDbContext, WorkflowInstance entity, string compressedJson, CancellationToken cancellationToken)
    {        
        var payloadPersistence = GetPayloadPersistence();
        if (string.IsNullOrWhiteSpace(payloadPersistence.Key) || payloadPersistence.Value == WorkflowPayloadPersistenceMode.Internal)
        {
            SetRawData(managementElsaDbContext, entity, compressedJson);
            return;
        }

        if (payloadPersistence.Value == WorkflowPayloadPersistenceMode.Hybrid)
        {
            SetRawData(managementElsaDbContext, entity, compressedJson);
        }

        var payloadReference = await payloadStoreManager.Set(entity.Id, compressedJson, payloadPersistence.Key, cancellationToken);
        managementElsaDbContext.Entry(entity).Reference(x => x.DataReference).CurrentValue = payloadReference;

        if (payloadReference is not null && payloadPersistence.Value == WorkflowPayloadPersistenceMode.ExternalPreferred)
        {
            SetRawData(managementElsaDbContext, entity, rawValue: null);
        }
    }

    private KeyValuePair<string, WorkflowPayloadPersistenceMode> GetPayloadPersistence()
    {
        return payloadOptions.Value.WorkflowInstancesPersistence ?? _options.Value.DefaultPayloadPersistence ?? new();
    }

    private static void SetRawData(ElsaDbContextBase dbContext, Entity entity, string? rawValue)
        => dbContext.Entry(entity).Property("Data").CurrentValue = rawValue;
}
