using Elsa.Common.Entities;
using System.Text.Json.Serialization;

namespace Elsa.Connections.Models;

public sealed class IntegrationConnection : Entity
{
    public string EnvironmentId { get; set; } = default!;
    public string ProviderId { get; set; } = default!;
    public string ProviderAccountId { get; set; } = default!;
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Active;
    public long Revision { get; set; } = 1;
    [JsonIgnore]
    public string? CurrentSecretName { get; set; }

    [JsonIgnore]
    public string? CurrentGenerationId { get; set; }

    [JsonIgnore]
    public string? OperationId { get; set; }
    [JsonIgnore]
    public long OperationExpectedRevision { get; set; }
    [JsonIgnore]
    public long OperationFence { get; set; }
    [JsonIgnore]
    public DateTimeOffset? OperationLeaseExpiresAt { get; set; }
    [JsonIgnore]
    public CredentialOperationStatus OperationStatus { get; set; }
    [JsonIgnore]
    public string? OperationSourceGenerationId { get; set; }
    [JsonIgnore]
    public string? PlannedSecretName { get; set; }
    [JsonIgnore]
    public string? PlannedGenerationId { get; set; }
    [JsonIgnore]
    public string? StagedSecretName { get; set; }
    [JsonIgnore]
    public string? StagedGenerationId { get; set; }
    [JsonIgnore]
    public string? LastSafeErrorCode { get; set; }
}

public enum ConnectionStatus
{
    Active,
    Disconnected,
    RecoveryRequired
}

public enum CredentialOperationStatus
{
    None,
    Claimed,
    ProviderCallStarted,
    CredentialReceived,
    Staged,
    Completed,
    RecoveryRequired
}
