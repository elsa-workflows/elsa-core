using System.IO.Compression;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ImportFiles;

internal static class WorkflowDefinitionImportFileReader
{
    public static async Task<IReadOnlyCollection<WorkflowDefinitionModel>> ReadAsync(IFormFileCollection files, IApiSerializer apiSerializer, Func<bool> hasResponseStarted, CancellationToken cancellationToken)
    {
        var models = new List<WorkflowDefinitionModel>();

        foreach (var file in files)
        {
            if (hasResponseStarted())
                return models;

            await using var fileStream = file.OpenReadStream();

            if (file.ContentType == "application/json")
            {
                models.Add(await ReadJsonStreamAsync(fileStream, apiSerializer, cancellationToken));
                continue;
            }

            using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

            foreach (var entry in zipArchive.Entries)
            {
                if (hasResponseStarted())
                    return models;

                if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                await using var jsonStream = entry.Open();
                models.Add(await ReadJsonStreamAsync(jsonStream, apiSerializer, cancellationToken));
            }
        }

        return models;
    }

    private static async Task<WorkflowDefinitionModel> ReadJsonStreamAsync(Stream jsonStream, IApiSerializer apiSerializer, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(jsonStream);
        var json = await reader.ReadToEndAsync(cancellationToken);
        return apiSerializer.Deserialize<WorkflowDefinitionModel>(json);
    }
}
