using System.Diagnostics.CodeAnalysis;
using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Common.Entities;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using static Elsa.Persistence.EFCore.Modules.Management.EFCoreWorkflowDefinitionStore;

namespace Elsa.Persistence.EFCore.Modules.Management;

public sealed class WorkflowPayloadSaveHandler(
    IPayloadSerializer payloadSerializer,
    IWorkflowStateSerializer workflowStateSerializer,
    IPayloadManager payloadManager,
    ICompressionCodecResolver compressionCodecResolver,
    IOptions<ManagementOptions> _options,
    IOptions<PayloadOptions> payloadOptions
)

: IEntitySavingHandler
{
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.SerializeAsync(WorkflowState, CancellationToken)")]
    public async ValueTask HandleAsync(ElsaDbContextBase dbContext, EntityEntry entry, CancellationToken cancellationToken = default)
    {
        var compressionAlgorithm = _options.Value.CompressionAlgorithm ?? nameof(None);
        string json;
        string dataReferencePropertyName;
        PayloadPersistenceOption? persistenceOption;

        if (entry.Entity is WorkflowInstance workflowInstance)
        {
            json = GetJson(workflowInstance.WorkflowState);
            dataReferencePropertyName = nameof(WorkflowInstance.DataReference);
            persistenceOption = payloadOptions.Value.WorkflowInstancesPersistence;
            dbContext.Entry(workflowInstance).Property("DataCompressionAlgorithm").CurrentValue = compressionAlgorithm;
        }
        else if (entry.Entity is WorkflowDefinition workflowDefinition)
        {
            json = GetJson(workflowDefinition);
            dataReferencePropertyName = nameof(WorkflowDefinition.DataReference);
            persistenceOption = payloadOptions.Value.WorkflowDefinitionsPersistence;
        }
        else
        {
            return;
        }

        var compressionCodec = compressionCodecResolver.Resolve(compressionAlgorithm);
        var compressedJson = await compressionCodec.CompressAsync(json, cancellationToken);        

        await SetPayloadData(
            dbContext, 
            (Entity)entry.Entity, 
            dataReferencePropertyName, 
            compressedJson, 
            compressionAlgorithm, 
            persistenceOption,
            cancellationToken
        );
    }

    private string GetJson(WorkflowState state)
        => workflowStateSerializer.Serialize(state);

    private string GetJson(WorkflowDefinition definition)
    {
        var data = new WorkflowDefinitionState(definition.Options, definition.Variables, definition.Inputs, definition.Outputs, definition.Outcomes, definition.CustomProperties);
        return payloadSerializer.Serialize(data);
    }

    private async ValueTask SetPayloadData(ElsaDbContextBase managementElsaDbContext, Entity entity, string dataReferencePropertyName, string compressedJson, string compressionAlgorithm, PayloadPersistenceOption? persistenceOption, CancellationToken cancellationToken)
    {
        var payloadPersistence = GetPayloadPersistence(persistenceOption);
        if (string.IsNullOrWhiteSpace(payloadPersistence.PayloadTypeIdentifier) || payloadPersistence.Mode == PayloadPersistenceMode.Internal)
        {
            SetRawData(managementElsaDbContext, entity, compressedJson);
            return;
        }

        if (payloadPersistence.Mode == PayloadPersistenceMode.Hybrid)
        {
            SetRawData(managementElsaDbContext, entity, compressedJson);
        }

        var payloadReference = await payloadManager.Set(entity.Id, compressedJson, payloadPersistence.PayloadTypeIdentifier, compressionAlgorithm, cancellationToken);
        managementElsaDbContext.Entry(entity).Reference(dataReferencePropertyName).CurrentValue = payloadReference;

        if (payloadReference is not null && payloadPersistence.Mode == PayloadPersistenceMode.ExternalPreferred)
        {
            SetRawData(managementElsaDbContext, entity, rawValue: null);
        }
    }

    private PayloadPersistenceOption GetPayloadPersistence(PayloadPersistenceOption? option)
    {
        return option ?? _options.Value.DefaultPayloadPersistence ?? new();
    }

    private static void SetRawData(ElsaDbContextBase dbContext, Entity entity, string? rawValue)
        => dbContext.Entry(entity).Property("Data").CurrentValue = rawValue;
}
