using System.Net;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Studio.PlatformIntegration.Contracts;
using Elsa.Studio.PlatformIntegration.Serialization;

namespace Elsa.Studio.PlatformIntegration.Services;

internal sealed class PlatformArtifactSubmitClient(HttpClient httpClient) : IPlatformArtifactSubmitClient
{
    private readonly PlatformArtifactEnvelopeValidator _validator = new();

    public async Task<PlatformSubmitResult> SubmitAsync(PlatformWorkflowSubmitPackage package, PlatformSubmitOptions options, CancellationToken cancellationToken = default)
    {
        ValidateOptions(options);

        var artifact = BuildArtifactZip(package);
        var upload = await CreateUploadAsync(artifact, options, cancellationToken);
        if (!upload.Succeeded)
            return upload.Result!;

        var uploadContent = await UploadContentAsync(upload.Upload!, artifact, options, cancellationToken);
        if (uploadContent is not null)
            return uploadContent;

        var complete = await CompleteUploadAsync(upload.Upload!.UploadId, options, cancellationToken);
        if (!complete.Result.Succeeded)
            return complete.Result;

        var completedArtifact = complete.Artifact!;
        var revision = await CreateRevisionAsync(package, completedArtifact, options, cancellationToken);
        return revision with
        {
            ArtifactId = complete.Result.ArtifactId,
            ArtifactDigest = complete.Result.ArtifactDigest,
            RegisteredAt = complete.Result.RegisteredAt,
            ArtifactRecordId = completedArtifact.Id
        };
    }

    private async Task<UploadStep> CreateUploadAsync(
        BuiltArtifact artifact,
        PlatformSubmitOptions options,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUploadUri(options))
        {
            Content = JsonContent.Create(
                new CreateArtifactUploadRequest(
                    $"loom-recipe-{artifact.ArtifactId.Value[..12]}.zip",
                    "application/zip",
                    artifact.ZipBytes.Length,
                    DigestString(artifact.ArtifactId)),
                PlatformIntegrationJsonContext.Default.CreateArtifactUploadRequest)
        };

        if (options.ConfigureRequestAsync is not null)
            await options.ConfigureRequestAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var message = await SafeResponseMessageAsync(response, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created)
            return new UploadStep(null, ToResult(response.StatusCode, message));

        var upload = await response.Content.ReadFromJsonAsync(PlatformIntegrationJsonContext.Default.CreateArtifactUploadResponse, cancellationToken);
        return upload is null
            ? new UploadStep(null, new PlatformSubmitResult(PlatformSubmitStatus.RetryableError, "Platform upload session response could not be read."))
            : new UploadStep(upload, null);
    }

    private async Task<PlatformSubmitResult?> UploadContentAsync(
        CreateArtifactUploadResponse upload,
        BuiltArtifact artifact,
        PlatformSubmitOptions options,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, BuildUploadContentUri(options, upload.UploadId))
        {
            Content = new ByteArrayContent(artifact.ZipBytes)
        };
        request.Content.Headers.ContentType = new("application/zip");

        if (options.ConfigureRequestAsync is not null)
            await options.ConfigureRequestAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.NoContent
            ? null
            : ToResult(response.StatusCode, await SafeResponseMessageAsync(response, cancellationToken));
    }

    private async Task<CompleteUploadStep> CompleteUploadAsync(
        Guid uploadId,
        PlatformSubmitOptions options,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUploadCompleteUri(options, uploadId));

        if (options.ConfigureRequestAsync is not null)
            await options.ConfigureRequestAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var message = await SafeResponseMessageAsync(response, cancellationToken);
        if (response.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.OK))
            return new CompleteUploadStep(null, ToResult(response.StatusCode, message));

        CompleteArtifactUploadResponse? completed;
        try
        {
            completed = await response.Content.ReadFromJsonAsync(PlatformIntegrationJsonContext.Default.CompleteArtifactUploadResponse, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return new CompleteUploadStep(null, new PlatformSubmitResult(PlatformSubmitStatus.RetryableError, "Platform upload completion response could not be read."));
        }

        if (completed?.Artifact is null)
            return new CompleteUploadStep(null, new PlatformSubmitResult(PlatformSubmitStatus.ValidationFailed, "Platform upload completion did not return an artifact."));

        var status = response.StatusCode == HttpStatusCode.Created && completed.Created
            ? PlatformSubmitStatus.Submitted
            : PlatformSubmitStatus.Duplicate;
        return new CompleteUploadStep(
            completed.Artifact,
            new PlatformSubmitResult(
                status,
                status == PlatformSubmitStatus.Submitted ? "Submitted to Platform." : "Artifact already exists in Platform.",
                completed.Artifact.ArtifactId,
                $"{completed.Artifact.ContentDigest.Algorithm}:{completed.Artifact.ContentDigest.Value}",
                completed.Artifact.RegisteredAt,
                completed.Artifact.Id));
    }

    private async Task<PlatformSubmitResult> CreateRevisionAsync(
        PlatformWorkflowSubmitPackage package,
        WorkspaceArtifactResponse artifact,
        PlatformSubmitOptions options,
        CancellationToken cancellationToken)
    {
        var payload = new ArtifactReferencePayload(
            artifact.Id,
            artifact.ArtifactId,
            package.Envelope.ArtifactTypeId,
            artifact.ContentDigest);
        var requestBody = new WorkspaceDesiredStateRevisionRequest(
            BuildRevisionLabel(package, options),
            options.RevisionCommit,
            [
                new WorkspaceDesiredStateRecordRequest(
                    "ArtifactReference",
                    package.Envelope.DisplayMetadata.Name ?? package.Envelope.ArtifactId,
                    JsonSerializer.SerializeToElement(payload, PlatformIntegrationJsonContext.Default.ArtifactReferencePayload))
            ]);
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildRevisionUri(options))
        {
            Content = JsonContent.Create(requestBody, PlatformIntegrationJsonContext.Default.WorkspaceDesiredStateRevisionRequest)
        };

        if (options.ConfigureRequestAsync is not null)
            await options.ConfigureRequestAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var message = await SafeResponseMessageAsync(response, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created)
            return ToResult(response.StatusCode, message);

        var revision = await response.Content.ReadFromJsonAsync(PlatformIntegrationJsonContext.Default.WorkspaceDesiredStateRevisionResponse, cancellationToken);
        return revision is null
            ? new PlatformSubmitResult(PlatformSubmitStatus.RetryableError, "Platform revision response could not be read.")
            : new PlatformSubmitResult(PlatformSubmitStatus.Submitted, "Submitted to Platform.", RevisionId: revision.Id);
    }

    private static BuiltArtifact BuildArtifactZip(PlatformWorkflowSubmitPackage package)
    {
        const string manifestPath = "manifest/manifest.json";
        var payloadPath = $"payload/recipes/{SafePathSegment(package.Envelope.ArtifactId)}.json";
        var recipeRelativePath = payloadPath["payload/".Length..];
        var recipeVersion = package.Envelope.DisplayMetadata.Version;
        var manifest = new PlatformEnvironmentManifest(
            "platform.elsa.io/v1alpha1",
            "Environment",
            new PlatformManifestMetadata(
                package.Envelope.DisplayMetadata.Name,
                recipeVersion,
                null,
                package.Envelope.DisplayMetadata.Labels,
                package.Envelope.DisplayMetadata.Annotations),
            new PlatformManifestResources(
            [
                new PlatformRecipeManifestEntry(
                    package.Envelope.Producer.SourceReference ?? package.Envelope.ArtifactId,
                    recipeRelativePath,
                    recipeVersion,
                    [],
                    new Dictionary<string, string>())
            ]));

        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, PlatformIntegrationJsonContext.Default.PlatformEnvironmentManifest);
        var payloadBytes = Encoding.UTF8.GetBytes(package.WorkflowDefinitionJson);
        var contentEntries = new[]
        {
            Checksum(manifestPath, PlatformDeploymentArtifactEntryKind.Manifest, manifestBytes),
            Checksum(payloadPath, PlatformDeploymentArtifactEntryKind.Payload, payloadBytes)
        };
        var contentDigest = ComputeContentDigest(contentEntries);
        var metadata = new PlatformDeploymentArtifactMetadata(
            PlatformArtifactEnvelopeConstants.LayoutVersion,
            DigestString(contentDigest),
            package.PackagedAt,
            new PlatformDeploymentArtifactManifestMetadata(
                package.Envelope.DisplayMetadata.Name,
                recipeVersion,
                null,
                package.Envelope.DisplayMetadata.Labels,
                package.Envelope.DisplayMetadata.Annotations),
            [
                new PlatformDeploymentArtifactResourceSummary(
                    PlatformArtifactEnvelopeConstants.ElsaLoomRecipeArtifactType,
                    package.Envelope.Producer.SourceReference ?? package.Envelope.ArtifactId,
                    null,
                    recipeVersion,
                    package.Envelope.ContentDigest)
            ],
            contentDigest,
            "Elsa Studio",
            package.Envelope.DisplayMetadata.Source);
        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, PlatformIntegrationJsonContext.Default.PlatformDeploymentArtifactMetadata);
        var metadataEntry = Checksum("artifact.json", PlatformDeploymentArtifactEntryKind.Metadata, metadataBytes);
        var inventory = new PlatformDeploymentArtifactChecksumInventory("sha256", contentEntries.Concat([metadataEntry]).OrderBy(x => x.Path, StringComparer.Ordinal).ToList());
        var checksumBytes = JsonSerializer.SerializeToUtf8Bytes(inventory, PlatformIntegrationJsonContext.Default.PlatformDeploymentArtifactChecksumInventory);

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, manifestPath, manifestBytes);
            WriteEntry(archive, payloadPath, payloadBytes);
            WriteEntry(archive, "artifact.json", metadataBytes);
            WriteEntry(archive, "checksums.json", checksumBytes);
        }

        return new BuiltArtifact(contentDigest, output.ToArray());
    }

    private static PlatformArtifactDigest ComputeContentDigest(IEnumerable<PlatformDeploymentArtifactChecksumEntry> entries)
    {
        var canonical = entries
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .Select(entry => new
            {
                entry.Path,
                Kind = entry.Kind.ToString(),
                entry.Algorithm,
                Digest = entry.Digest,
                entry.Size
            })
            .ToArray();
        var json = JsonSerializer.Serialize(canonical, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return new PlatformArtifactDigest("sha256", Convert.ToHexString(hash).ToLowerInvariant());
    }

    private static PlatformDeploymentArtifactChecksumEntry Checksum(
        string path,
        PlatformDeploymentArtifactEntryKind kind,
        byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return new PlatformDeploymentArtifactChecksumEntry(path, kind, "sha256", Convert.ToHexString(hash).ToLowerInvariant(), bytes.LongLength);
    }

    private static void WriteEntry(ZipArchive archive, string path, byte[] bytes)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private async Task<string> SafeResponseMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return response.StatusCode == HttpStatusCode.Created ? "Submitted to Platform." : "Artifact already exists in Platform.";

        try
        {
            var problem = await response.Content.ReadFromJsonAsync(PlatformIntegrationJsonContext.Default.ProblemDetailsResponse, cancellationToken);
            return _validator.SafeMessage(problem?.Title ?? problem?.Detail ?? response.ReasonPhrase);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return _validator.SafeMessage(response.ReasonPhrase);
        }
    }

    private static void ValidateOptions(PlatformSubmitOptions options)
    {
        if (options.PlatformEndpoint is null)
            throw new InvalidOperationException("Platform endpoint is required before submitting to Platform.");
        if (options.WorkspaceId is null || options.WorkspaceId == Guid.Empty)
            throw new InvalidOperationException("Platform workspace is required before submitting to Platform.");
        if (options.ApplicationId is null || options.ApplicationId == Guid.Empty)
            throw new InvalidOperationException("Platform application is required before submitting to Platform.");
        if (options.EnvironmentId is null || options.EnvironmentId == Guid.Empty)
            throw new InvalidOperationException("Platform environment is required before submitting to Platform.");
    }

    private static Uri BuildUploadUri(PlatformSubmitOptions options) =>
        new($"{options.PlatformEndpoint!.AbsoluteUri.TrimEnd('/')}/api/workspaces/{options.WorkspaceId:D}/artifact-uploads");

    private static Uri BuildUploadContentUri(PlatformSubmitOptions options, Guid uploadId) =>
        new($"{options.PlatformEndpoint!.AbsoluteUri.TrimEnd('/')}/api/workspaces/{options.WorkspaceId:D}/artifact-uploads/{uploadId:D}/content");

    private static Uri BuildUploadCompleteUri(PlatformSubmitOptions options, Guid uploadId) =>
        new($"{options.PlatformEndpoint!.AbsoluteUri.TrimEnd('/')}/api/workspaces/{options.WorkspaceId:D}/artifact-uploads/{uploadId:D}/complete");

    private static Uri BuildRevisionUri(PlatformSubmitOptions options) =>
        new($"{options.PlatformEndpoint!.AbsoluteUri.TrimEnd('/')}/api/workspaces/{options.WorkspaceId:D}/deployments/applications/{options.ApplicationId:D}/environments/{options.EnvironmentId:D}/revisions");

    private static string BuildRevisionLabel(PlatformWorkflowSubmitPackage package, PlatformSubmitOptions options) =>
        string.IsNullOrWhiteSpace(options.RevisionLabel)
            ? $"Studio submit {package.Envelope.DisplayMetadata.Name ?? package.Envelope.ArtifactId} {package.PackagedAt:yyyyMMddHHmmss}"
            : options.RevisionLabel.Trim();

    private static string DigestString(PlatformArtifactDigest digest) =>
        $"{digest.Algorithm}:{digest.Value}";

    private static PlatformSubmitResult ToResult(HttpStatusCode statusCode, string message) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new PlatformSubmitResult(PlatformSubmitStatus.Unauthorized, message),
            HttpStatusCode.Conflict => new PlatformSubmitResult(PlatformSubmitStatus.Conflict, message),
            HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge => new PlatformSubmitResult(PlatformSubmitStatus.ValidationFailed, message),
            _ when (int)statusCode >= 500 => new PlatformSubmitResult(PlatformSubmitStatus.RetryableError, message),
            _ => new PlatformSubmitResult(PlatformSubmitStatus.ValidationFailed, message)
        };

    private static string SafePathSegment(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' or '.' ? character : '-');

        var result = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "recipe" : result;
    }

    private sealed record BuiltArtifact(PlatformArtifactDigest ArtifactId, byte[] ZipBytes);

    private sealed record UploadStep(CreateArtifactUploadResponse? Upload, PlatformSubmitResult? Result)
    {
        public bool Succeeded => Result is null && Upload is not null;
    }

    private sealed record CompleteUploadStep(WorkspaceArtifactResponse? Artifact, PlatformSubmitResult Result);
}
