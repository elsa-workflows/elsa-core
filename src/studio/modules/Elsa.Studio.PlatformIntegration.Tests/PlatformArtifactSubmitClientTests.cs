using System.Net;
using System.IO.Compression;
using Elsa.Studio.PlatformIntegration.Services;
using Xunit;

namespace Elsa.Studio.PlatformIntegration.Tests;

public sealed class PlatformArtifactSubmitClientTests
{
    private readonly PlatformSubmitOptions _options = new()
    {
        PlatformEndpoint = new Uri("https://platform.example.test"),
        WorkspaceId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        ApplicationId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
        EnvironmentId = Guid.Parse("40000000-0000-0000-0000-000000000001")
    };

    [Fact]
    public async Task Uploads_artifact_and_creates_desired_state_revision()
    {
        var handler = new RecordingHandler(
            Response(HttpStatusCode.Created, """
                {
                  "uploadId": "50000000-0000-0000-0000-000000000001",
                  "status": "Pending",
                  "expiresAt": "2026-05-29T08:30:00Z",
                  "maxUploadBytes": 52428800
                }
                """),
            Response(HttpStatusCode.NoContent, ""),
            Response(HttpStatusCode.Created, """
                {
                  "uploadId": "50000000-0000-0000-0000-000000000001",
                  "status": "Completed",
                  "created": true,
                  "artifact": {
                    "id": "20000000-0000-0000-0000-000000000001",
                    "artifactId": "sha256:abc",
                    "contentDigest": { "algorithm": "sha256", "value": "abc" },
                    "registeredAt": "2026-05-29T08:00:00Z"
                  },
                  "diagnostics": []
                }
                """),
            Response(HttpStatusCode.Created, """
                {
                  "id": "60000000-0000-0000-0000-000000000001",
                  "workspaceId": "10000000-0000-0000-0000-000000000001",
                  "applicationId": "30000000-0000-0000-0000-000000000001",
                  "environmentId": "40000000-0000-0000-0000-000000000001",
                  "label": "Studio submit",
                  "commit": null,
                  "createdAt": "2026-05-29T08:00:00Z"
                }
                """));
        var client = new PlatformArtifactSubmitClient(new HttpClient(handler));

        var result = await client.SubmitAsync(Package(), _options);

        Assert.Equal(PlatformSubmitStatus.Submitted, result.Status);
        Assert.Equal("sha256:abc", result.ArtifactId);
        Assert.Equal("sha256:abc", result.ArtifactDigest);
        Assert.Equal(Guid.Parse("20000000-0000-0000-0000-000000000001"), result.ArtifactRecordId);
        Assert.Equal(Guid.Parse("60000000-0000-0000-0000-000000000001"), result.RevisionId);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal(new Uri("https://platform.example.test/api/workspaces/10000000-0000-0000-0000-000000000001/artifact-uploads"), handler.Requests[0].RequestUri);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal(new Uri("https://platform.example.test/api/workspaces/10000000-0000-0000-0000-000000000001/artifact-uploads/50000000-0000-0000-0000-000000000001/content"), handler.Requests[1].RequestUri);
        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.Equal(HttpMethod.Post, handler.Requests[3].Method);
        Assert.Equal(new Uri("https://platform.example.test/api/workspaces/10000000-0000-0000-0000-000000000001/deployments/applications/30000000-0000-0000-0000-000000000001/environments/40000000-0000-0000-0000-000000000001/revisions"), handler.Requests[3].RequestUri);
        Assert.Contains("ArtifactReference", handler.Requests[3].Body);
        Assert.Contains("20000000-0000-0000-0000-000000000001", handler.Requests[3].Body);

        using var zip = new ZipArchive(new MemoryStream(handler.Requests[1].BodyBytes));
        Assert.Contains(zip.Entries, x => x.FullName == "artifact.json");
        Assert.Contains(zip.Entries, x => x.FullName == "checksums.json");
        Assert.Contains(zip.Entries, x => x.FullName == "manifest/manifest.json");
        Assert.Contains(zip.Entries, x => x.FullName.StartsWith("payload/recipes/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Treats_existing_uploaded_artifact_as_success_when_revision_is_created()
    {
        var handler = new RecordingHandler(
            Response(HttpStatusCode.Created, """
                {
                  "uploadId": "50000000-0000-0000-0000-000000000001",
                  "status": "Pending",
                  "expiresAt": "2026-05-29T08:30:00Z",
                  "maxUploadBytes": 52428800
                }
                """),
            Response(HttpStatusCode.NoContent, ""),
            Response(HttpStatusCode.OK, """
                {
                  "uploadId": "50000000-0000-0000-0000-000000000001",
                  "status": "Completed",
                  "created": false,
                  "artifact": {
                    "id": "20000000-0000-0000-0000-000000000001",
                    "artifactId": "sha256:abc",
                    "contentDigest": { "algorithm": "sha256", "value": "abc" },
                    "registeredAt": "2026-05-29T08:00:00Z"
                  },
                  "diagnostics": []
                }
                """),
            Response(HttpStatusCode.Created, """
                {
                  "id": "60000000-0000-0000-0000-000000000001",
                  "workspaceId": "10000000-0000-0000-0000-000000000001",
                  "applicationId": "30000000-0000-0000-0000-000000000001",
                  "environmentId": "40000000-0000-0000-0000-000000000001",
                  "label": "Studio submit",
                  "commit": null,
                  "createdAt": "2026-05-29T08:00:00Z"
                }
                """));
        var client = new PlatformArtifactSubmitClient(new HttpClient(handler));

        var result = await client.SubmitAsync(Package(), _options);

        Assert.Equal(PlatformSubmitStatus.Submitted, result.Status);
        Assert.True(result.Succeeded);
        Assert.Equal(Guid.Parse("60000000-0000-0000-0000-000000000001"), result.RevisionId);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, PlatformSubmitStatus.Conflict)]
    [InlineData(HttpStatusCode.BadRequest, PlatformSubmitStatus.ValidationFailed)]
    [InlineData(HttpStatusCode.Unauthorized, PlatformSubmitStatus.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError, PlatformSubmitStatus.RetryableError)]
    public async Task Maps_platform_responses_to_safe_submit_states(HttpStatusCode statusCode, PlatformSubmitStatus expectedStatus)
    {
        var handler = new RecordingHandler(Response(statusCode, """{"title":"Bearer token rejected"}"""));
        var client = new PlatformArtifactSubmitClient(new HttpClient(handler));

        var result = await client.SubmitAsync(Package(), _options);

        Assert.Equal(expectedStatus, result.Status);
        Assert.DoesNotContain("Bearer", result.Message);
        Assert.Contains("[redacted]", result.Message);
    }

    private static PlatformWorkflowSubmitPackage Package() =>
        new(
            new PlatformArtifactEnvelope(
                "elsa.loom.recipe:payment-retry:abc",
                PlatformArtifactEnvelopeConstants.EnvelopeVersion,
                PlatformArtifactEnvelopeConstants.ElsaLoomRecipeArtifactType,
                PlatformArtifactEnvelopeConstants.DefaultArtifactSchemaVersion,
                new PlatformArtifactDigest("sha256", new string('a', 64)),
                null,
                new PlatformArtifactPayloadReference("producer-managed", "studio://loom-recipes/payment-retry/snapshots/abc"),
                new PlatformArtifactProducer("studio", "Elsa Studio", SourceReference: "payment-retry"),
                new PlatformArtifactDisplayMetadata("Payment Retry", "42", null, new Dictionary<string, string>(), new Dictionary<string, string>(), "studio://workflows/payment-retry"),
                [],
                []),
            """
            {
              "schemaVersion": "1.0",
              "id": "payment-retry",
              "name": "Payment Retry",
              "steps": [
                {
                  "id": "upsert-payment-retry",
                  "type": "workflowDefinition.upsert",
                  "publish": true,
                  "payload": {
                    "definitionId": "payment-retry",
                    "name": "Payment Retry",
                    "root": {}
                  }
                }
              ]
            }
            """,
            DateTimeOffset.UtcNow);

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string content, string contentType = "application/json") =>
        new(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, contentType)
        };

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var bodyBytes = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, System.Text.Encoding.UTF8.GetString(bodyBytes), bodyBytes));
            return _responses.Dequeue();
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri RequestUri, string Body, byte[] BodyBytes);
}
