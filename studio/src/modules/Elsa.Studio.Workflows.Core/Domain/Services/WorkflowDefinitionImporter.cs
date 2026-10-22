using System.IO.Compression;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow definition service that uses a remote backend to retrieve workflow definitions.
/// </summary>
public class WorkflowDefinitionImporter : IWorkflowDefinitionImporter
{
    private readonly IBackendApiClientProvider backendApiClientProvider;
    private readonly IMediator mediator;
    private readonly IWorkflowJsonDetector workflowJsonDetector;
    private readonly IRemoteFeatureProvider? remoteFeatureProvider;
    private readonly INativeCompensationDraftGraphClassifier nativeDraftGraphClassifier;

    public WorkflowDefinitionImporter(IBackendApiClientProvider backendApiClientProvider, IMediator mediator, IWorkflowJsonDetector workflowJsonDetector)
        : this(backendApiClientProvider, mediator, workflowJsonDetector, null, new NativeCompensationDraftGraphClassifier())
    {
    }

    public WorkflowDefinitionImporter(
        IBackendApiClientProvider backendApiClientProvider,
        IMediator mediator,
        IWorkflowJsonDetector workflowJsonDetector,
        IRemoteFeatureProvider? remoteFeatureProvider)
        : this(backendApiClientProvider, mediator, workflowJsonDetector, remoteFeatureProvider, new NativeCompensationDraftGraphClassifier())
    {
    }

    public WorkflowDefinitionImporter(
        IBackendApiClientProvider backendApiClientProvider,
        IMediator mediator,
        IWorkflowJsonDetector workflowJsonDetector,
        IRemoteFeatureProvider? remoteFeatureProvider,
        INativeCompensationDraftGraphClassifier nativeDraftGraphClassifier)
    {
        this.backendApiClientProvider = backendApiClientProvider;
        this.mediator = mediator;
        this.workflowJsonDetector = workflowJsonDetector;
        this.remoteFeatureProvider = remoteFeatureProvider;
        this.nativeDraftGraphClassifier = nativeDraftGraphClassifier;
    }

    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default)
    {
        return await backendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowImportResult>> ImportFilesAsync(IReadOnlyList<IBrowserFile> files, ImportOptions? options = null)
    {
        var maxAllowedSize = options?.MaxAllowedSize ?? 1024 * 1024 * 10; // 10 MB
        var results = new List<WorkflowImportResult>();

        foreach (var file in files)
        {
            await mediator.NotifyAsync(new ImportingFile(file));
            await using var stream = file.OpenReadStream(maxAllowedSize);

            if (file.ContentType == MediaTypeNames.Application.Zip || file.Name.EndsWith(".zip"))
            {
                var importZipFileResults = await ImportZipFileAsync(stream, options);
                results.AddRange(importZipFileResults);
                await mediator.NotifyAsync(new ImportedFile(file));
            }

            else if (file.ContentType == MediaTypeNames.Application.Json || file.Name.EndsWith(".json"))
            {
                var importFromStreamResult = await ImportFromStreamAsync(file.Name, stream, options);
                results.Add(importFromStreamResult);
                await mediator.NotifyAsync(new ImportedFile(file));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IList<WorkflowImportResult>> ImportZipFileAsync(Stream stream, ImportOptions? options)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        var importResultList = new List<WorkflowImportResult>();

        try
        {
            var zipArchive = new ZipArchive(memoryStream);
            foreach (var entry in zipArchive.Entries.Where(x => !x.FullName.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase)))
            {
                if (entry.FullName.EndsWith(".json"))
                {
                    await using var entryStream = entry.Open();
                    var result = await ImportFromStreamAsync(entry.Name, entryStream, options);

                    importResultList.Add(result);
                }
                else if (entry.FullName.EndsWith(".zip"))
                {
                    await using var entryStream = entry.Open();
                    var results = await ImportZipFileAsync(entryStream, options);
                    importResultList.AddRange(results);
                }
            }
        }
        catch (Exception e)
        {
            if (options?.ErrorCallback != null)
                await options.ErrorCallback(e);
            importResultList.Add(new()
            {
                Failure = new(e.ToUserMessage(), WorkflowImportFailureType.Exception)
            });
        }

        return importResultList;
    }

    /// <inheritdoc />
    public async Task<WorkflowImportResult> ImportFromStreamAsync(string fileName, Stream stream, ImportOptions? options)
    {
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return await ImportJsonAsync(fileName, json, options);
    }

    /// <inheritdoc />
    public async Task<WorkflowImportResult> ImportJsonAsync(string fileName, string json, ImportOptions? options)
    {
        var jsonSerializerOptions = CreateJsonSerializerOptions();
        var backendContextKey = backendApiClientProvider.ContextKey;

        try
        {
            await mediator.NotifyAsync(new ImportingJson(json));

            if (!workflowJsonDetector.IsWorkflowSchema(json))
            {
                return new()
                {
                    FileName = fileName,
                    Failure = new("Invalid schema", WorkflowImportFailureType.InvalidSchema)
                };
            }

            var model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, jsonSerializerOptions)!;

            if (options?.DefinitionId != null)
                model.DefinitionId = options.DefinitionId;

            await mediator.NotifyAsync(new ImportingWorkflowDefinition(model));
            var requestModel = WorkflowDefinitionModelSnapshot.WithRoot(model, model.Root);
            var analysis = await nativeDraftGraphClassifier.AnalyzeAsync(requestModel.Root);
            EnsureSameBackendContext(backendContextKey);
            if (analysis.MustRejectProtectedWrite)
                throw new InvalidOperationException("The imported workflow contains a native compensation scope Studio cannot safely classify or import.");
            var containsCompensationDraft = analysis.ContainsCompensationScope;
            if (containsCompensationDraft)
            {
                EnsureSameBackendContext(backendContextKey);
                if (!await NativeCompensationDraftCapability.IsEnabledForContextAsync(remoteFeatureProvider, backendApiClientProvider, backendContextKey))
                    throw backendApiClientProvider.ContextKey == backendContextKey
                        ? new InvalidOperationException("This server does not advertise native compensation draft authoring. The workflow was not imported.")
                        : new OperationCanceledException("The selected backend changed while importing the workflow.");
            }

            EnsureSameBackendContext(backendContextKey);
            var api = await GetApiAsync();
            EnsureSameBackendContext(backendContextKey);
            WorkflowDefinition newWorkflowDefinition;
            if (containsCompensationDraft)
            {
                var binding = (IBackendRequestContextBinding)backendApiClientProvider;
                using (binding.BindRequestContext(backendContextKey))
                    newWorkflowDefinition = await api.ImportAsync(requestModel);
            }
            else
                newWorkflowDefinition = await api.ImportAsync(requestModel);

            EnsureSameBackendContext(backendContextKey);
            if (!NativeCompensationDraftDocumentContext.TryBind(newWorkflowDefinition, backendContextKey))
                throw new OperationCanceledException("The imported workflow is already bound to a different backend context.");

            if (options?.ImportedCallback != null)
                await options.ImportedCallback(newWorkflowDefinition);

            await mediator.NotifyAsync(new ImportedWorkflowDefinition(newWorkflowDefinition));
            await mediator.NotifyAsync(new ImportedJson(json));

            return new()
            {
                FileName = fileName,
                WorkflowDefinition = newWorkflowDefinition
            };
        }
        catch (Exception e)
        {
            if (options?.ErrorCallback != null)
                await options.ErrorCallback(e);
            return new()
            {
                FileName = fileName,
                Failure = new(e.ToUserMessage(), WorkflowImportFailureType.Exception)
            };
        }
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new VersionOptionsJsonConverter());
        return options;
    }

    private void EnsureSameBackendContext(string expected)
    {
        if (!string.Equals(backendApiClientProvider.ContextKey, expected, StringComparison.Ordinal))
            throw new OperationCanceledException("The selected backend changed while importing the workflow.");
    }
}
