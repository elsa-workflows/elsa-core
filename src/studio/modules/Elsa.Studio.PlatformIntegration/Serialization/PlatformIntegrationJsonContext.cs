using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Studio.PlatformIntegration.Serialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(WorkspaceArtifactRegistrationRequest))]
[JsonSerializable(typeof(WorkspaceArtifactResponse))]
[JsonSerializable(typeof(PlatformDeploymentArtifactMetadata))]
[JsonSerializable(typeof(PlatformDeploymentArtifactChecksumInventory))]
[JsonSerializable(typeof(PlatformEnvironmentManifest))]
[JsonSerializable(typeof(CreateArtifactUploadRequest))]
[JsonSerializable(typeof(CreateArtifactUploadResponse))]
[JsonSerializable(typeof(CompleteArtifactUploadResponse))]
[JsonSerializable(typeof(WorkspaceDesiredStateRevisionRequest))]
[JsonSerializable(typeof(WorkspaceDesiredStateRevisionResponse))]
[JsonSerializable(typeof(ArtifactReferencePayload))]
[JsonSerializable(typeof(ProblemDetailsResponse))]
internal sealed partial class PlatformIntegrationJsonContext : JsonSerializerContext;
