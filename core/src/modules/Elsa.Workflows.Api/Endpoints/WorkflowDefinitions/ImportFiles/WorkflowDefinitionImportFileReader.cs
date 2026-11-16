using System.IO.Compression;
using System.Text.Json;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ImportFiles;

internal static class WorkflowDefinitionImportFileReader
{
    internal record ReadResult(IReadOnlyCollection<WorkflowDefinitionModel> Models, string? ErrorMessage = null)
    {
        public static ReadResult Failure(string message) => new([], message);
    }

    public static async Task<ReadResult> ReadAsync(IFormFileCollection files, IApiSerializer apiSerializer, Func<bool> hasResponseStarted, CancellationToken cancellationToken)
    {
        var models = new List<WorkflowDefinitionModel>();

        foreach (var file in files)
        {
            if (hasResponseStarted())
                return new(models);

            await using var fileStream = file.OpenReadStream();

            if (file.ContentType == "application/json")
            {
                var result = await ReadJsonStreamAsync(() => fileStream, file.FileName, apiSerializer, cancellationToken);
                if (result.ErrorMessage != null)
                    return result;

                models.AddRange(result.Models);
                continue;
            }

            // Even an empty ZIP needs a 22-byte end-of-central-directory record. Reject
            // shorter form streams before ZipArchive attempts to seek before their start.
            if (fileStream.CanSeek && fileStream.Length < 22)
                return ReadResult.Failure($"{file.FileName} is not a valid workflow archive.");

            ZipArchive zipArchive;
            try
            {
                zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            }
            catch (InvalidDataException)
            {
                return ReadResult.Failure($"{file.FileName} is not a valid workflow archive.");
            }

            using (zipArchive)
            {
                IReadOnlyCollection<ZipArchiveEntry> entries;
                try
                {
                    entries = zipArchive.Entries;
                }
                catch (InvalidDataException)
                {
                    return ReadResult.Failure($"{file.FileName} is not a valid workflow archive.");
                }

                foreach (var entry in entries)
                {
                    if (hasResponseStarted())
                        return new(models);

                    if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var result = await ReadJsonStreamAsync(entry.Open, $"{entry.FullName} in {file.FileName}", apiSerializer, cancellationToken, file.FileName);
                    if (result.ErrorMessage != null)
                        return result;

                    models.AddRange(result.Models);
                }
            }
        }

        return new(models);
    }

    private static async Task<ReadResult> ReadJsonStreamAsync(Func<Stream> openStream, string sourceName, IApiSerializer apiSerializer, CancellationToken cancellationToken, string? archiveName = null)
    {
        string json;
        try
        {
            await using var jsonStream = openStream();
            using var reader = new StreamReader(jsonStream);
            json = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (InvalidDataException) when (archiveName != null)
        {
            return ReadResult.Failure($"{archiveName} is not a valid workflow archive.");
        }

        try
        {
            var model = apiSerializer.Deserialize<WorkflowDefinitionModel>(json);
            return model != null ? new([model]) : InvalidDefinition();
        }
        catch (JsonException)
        {
            return InvalidDefinition();
        }

        ReadResult InvalidDefinition() => ReadResult.Failure($"{sourceName} is not a valid workflow definition.");
    }
}
