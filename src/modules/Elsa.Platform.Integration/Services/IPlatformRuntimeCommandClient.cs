using Elsa.Platform.Integration.Models;

namespace Elsa.Platform.Integration.Services;

public interface IPlatformRuntimeCommandClient
{
    Task<IReadOnlyList<PlatformRuntimeCommand>> PollAsync(CancellationToken cancellationToken = default);

    Task<PlatformRuntimeCommandClaimResponse?> ClaimAsync(Guid commandId, CancellationToken cancellationToken = default);

    Task<Stream> DownloadArtifactAsync(PlatformRuntimeCommand command, PlatformArtifactItem artifact, string leaseToken, CancellationToken cancellationToken = default);

    Task ReportProgressAsync(Guid commandId, string leaseToken, string status, int? percentComplete, string message, CancellationToken cancellationToken = default);

    Task CompleteAsync(Guid commandId, PlatformRuntimeCommandCompleteRequest request, CancellationToken cancellationToken = default);

    Task FailAsync(Guid commandId, PlatformRuntimeCommandFailRequest request, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid commandId, PlatformRuntimeCommandRejectRequest request, CancellationToken cancellationToken = default);
}
