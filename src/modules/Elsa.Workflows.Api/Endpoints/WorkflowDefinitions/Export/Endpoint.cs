using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Export;

/// <summary>
/// Exports the specified workflow definition as JSON download.
/// </summary>
[UsedImplicitly]
internal class Export(IWorkflowDefinitionExporter exporter) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/bulk-actions/export/workflow-definitions", "/workflow-definitions/{definitionId}/export");
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        ConfigurePermissions("read:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.DefinitionId != null)
        {
            var versionOptions = string.IsNullOrEmpty(request.VersionOptions) ? VersionOptions.Latest : VersionOptions.FromString(request.VersionOptions);
            var result = await exporter.ExportAsync(request.DefinitionId, versionOptions, request.IncludeConsumingWorkflows, cancellationToken);

            if (result == null)
            {
                await Send.NotFoundAsync(cancellationToken);
                return;
            }

            await Send.BytesAsync(result.Data, result.FileName, cancellation: cancellationToken);
        }
        else if (request.Ids != null)
        {
            var result = await exporter.ExportManyAsync(request.Ids, request.IncludeConsumingWorkflows, cancellationToken);

            if (result == null)
            {
                await Send.NoContentAsync(cancellationToken);
                return;
            }

            await Send.BytesAsync(result.Data, result.FileName, cancellation: cancellationToken);
        }
        else
        {
            await Send.NoContentAsync(cancellationToken);
        }
    }
}