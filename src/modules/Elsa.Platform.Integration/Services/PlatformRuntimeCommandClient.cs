using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Platform.Integration.Models;
using Elsa.Platform.Integration.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Platform.Integration.Services;

public class PlatformRuntimeCommandClient(HttpClient httpClient, IOptions<ElsaPlatformIntegrationOptions> options) : IPlatformRuntimeCommandClient
{
    private const string EngineSecretHeaderName = "X-Elsa-Engine-Secret";
    private const string LeaseHeaderName = "X-Elsa-Command-Lease";
    private const string WorkerHeaderName = "X-Elsa-Worker-Id";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ElsaPlatformIntegrationOptions _options = options.Value;

    public async Task<IReadOnlyList<PlatformRuntimeCommand>> PollAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, BuildUri($"/deployments/runtime/engines/{_options.EngineId:D}/commands"));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Elsa Platform runtime command poll was not authorized.");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PlatformRuntimeCommandListResponse>(JsonOptions, cancellationToken);
        return body?.Commands ?? [];
    }

    public async Task<PlatformRuntimeCommandClaimResponse?> ClaimAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        using var response = await SendJsonAsync(
            BuildUri($"/deployments/runtime/commands/{commandId:D}/claim"),
            new PlatformRuntimeCommandClaimRequest(_options.EngineId, _options.WorkerId, (int)_options.ClaimLeaseDuration.TotalSeconds),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformRuntimeCommandClaimResponse>(JsonOptions, cancellationToken);
    }

    public async Task<Stream> DownloadArtifactAsync(
        PlatformRuntimeCommand command,
        PlatformArtifactItem artifact,
        string leaseToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifact.DownloadUrl))
            throw new InvalidOperationException("Platform runtime command artifact does not include a download URL.");

        using var request = CreateRequest(HttpMethod.Get, BuildUri(artifact.DownloadUrl));
        request.Headers.Add(LeaseHeaderName, leaseToken);
        request.Headers.Add(WorkerHeaderName, _options.WorkerId);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new MemoryStream();
        await CopyBoundedAsync(stream, buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public Task ReportProgressAsync(
        Guid commandId,
        string leaseToken,
        string status,
        int? percentComplete,
        string message,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(
            commandId,
            "progress",
            new PlatformRuntimeCommandProgressRequest(leaseToken, status, percentComplete, message),
            cancellationToken);

    public Task CompleteAsync(Guid commandId, PlatformRuntimeCommandCompleteRequest request, CancellationToken cancellationToken = default) =>
        SendMutationAsync(commandId, "complete", request, cancellationToken);

    public Task FailAsync(Guid commandId, PlatformRuntimeCommandFailRequest request, CancellationToken cancellationToken = default) =>
        SendMutationAsync(commandId, "fail", request, cancellationToken);

    public Task RejectAsync(Guid commandId, PlatformRuntimeCommandRejectRequest request, CancellationToken cancellationToken = default) =>
        SendMutationAsync(commandId, "reject", request, cancellationToken);

    private async Task SendMutationAsync<TRequest>(
        Guid commandId,
        string action,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(BuildUri($"/deployments/runtime/commands/{commandId:D}/{action}"), body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        if (!string.IsNullOrWhiteSpace(_options.EngineSecret))
            request.Headers.TryAddWithoutValidation(EngineSecretHeaderName, _options.EngineSecret);
        return request;
    }

    private async Task<HttpResponseMessage> SendJsonAsync<TRequest>(Uri uri, TRequest body, CancellationToken cancellationToken)
    {
        var request = CreateRequest(HttpMethod.Post, uri);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private Uri BuildUri(string path)
    {
        var endpoint = _options.PlatformEndpoint ?? throw new InvalidOperationException("Elsa Platform endpoint is required.");
        if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
            return absoluteUri;

        var relative = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? path
            : $"/api/workspaces/{_options.WorkspaceId:D}{path}";
        return new Uri($"{endpoint.AbsoluteUri.TrimEnd('/')}{relative}");
    }

    private async Task CopyBoundedAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                return;

            total += read;
            if (total > _options.MaxArtifactBytes)
                throw new InvalidOperationException("Elsa Platform artifact exceeds the configured runtime size limit.");

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }
}
