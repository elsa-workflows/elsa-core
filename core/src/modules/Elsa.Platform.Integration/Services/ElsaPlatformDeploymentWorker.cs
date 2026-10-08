using Elsa.Platform.Integration.Models;
using Elsa.Platform.Integration.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Platform.Integration.Services;

public class ElsaPlatformDeploymentWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ElsaPlatformIntegrationOptions> options,
    ILogger<ElsaPlatformDeploymentWorker> logger) : BackgroundService
{
    private readonly ElsaPlatformIntegrationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _options.Validate();
        if (!_options.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAvailableCommandsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Elsa Platform deployment worker failed while polling commands.");
            }

            await Task.Delay(_options.PollInterval, stoppingToken);
        }
    }

    private async Task ProcessAvailableCommandsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IPlatformRuntimeCommandClient>();
        var applier = scope.ServiceProvider.GetRequiredService<IPlatformRecipeArtifactApplier>();
        var commands = await client.PollAsync(cancellationToken);

        foreach (var command in commands)
        {
            var claim = await client.ClaimAsync(command.Id, cancellationToken);
            if (claim is null)
                continue;

            await ProcessClaimedCommandAsync(client, applier, claim, cancellationToken);
        }
    }

    private async Task ProcessClaimedCommandAsync(
        IPlatformRuntimeCommandClient client,
        IPlatformRecipeArtifactApplier applier,
        PlatformRuntimeCommandClaimResponse claim,
        CancellationToken cancellationToken)
    {
        var command = claim.Command;
        if (command.Action is not PlatformRuntimeCommandAction.Deploy and not PlatformRuntimeCommandAction.Rollback)
        {
            await client.RejectAsync(
                command.Id,
                new PlatformRuntimeCommandRejectRequest(
                    claim.LeaseToken,
                    [PlatformDiagnosticSanitizer.Error("elsa-platform.command-unsupported", "Runtime command action is not supported by this Elsa runtime.")]),
                cancellationToken);
            return;
        }

        if (command.Artifacts is not { Count: > 0 })
        {
            await client.RejectAsync(
                command.Id,
                new PlatformRuntimeCommandRejectRequest(
                    claim.LeaseToken,
                    [PlatformDiagnosticSanitizer.Error("elsa-platform.artifact-missing", "Runtime command did not include deployment artifacts.")]),
                cancellationToken);
            return;
        }

        var outcomes = new List<PlatformArtifactOutcome>();
        try
        {
            foreach (var artifact in command.Artifacts)
            {
                await client.ReportProgressAsync(command.Id, claim.LeaseToken, "downloading", 20, "Downloading recipe artifact.", cancellationToken);
                await using var artifactZip = await client.DownloadArtifactAsync(command, artifact, claim.LeaseToken, cancellationToken);
                await client.ReportProgressAsync(command.Id, claim.LeaseToken, "applying", 60, "Applying recipe artifact.", cancellationToken);
                var result = await applier.ApplyAsync(command, artifact, artifactZip, cancellationToken);
                outcomes.Add(new PlatformArtifactOutcome(
                    artifact.ArtifactRecordId,
                    result.Status,
                    result.ObservedDigest,
                    result.RuntimeReference,
                    result.Diagnostics));

                if (!result.Succeeded)
                    break;
            }

            var failed = outcomes.FirstOrDefault(x => x.Status == PlatformArtifactStatus.Failed);
            if (failed is not null)
            {
                await client.FailAsync(
                    command.Id,
                    new PlatformRuntimeCommandFailRequest(
                        claim.LeaseToken,
                        failed.Diagnostics ?? [PlatformDiagnosticSanitizer.Error("elsa-platform.artifact-failed", "Recipe artifact apply failed.")],
                        outcomes),
                    cancellationToken);
                return;
            }

            var rejected = outcomes.FirstOrDefault(x => x.Status == PlatformArtifactStatus.Rejected);
            if (rejected is not null)
            {
                await client.RejectAsync(
                    command.Id,
                    new PlatformRuntimeCommandRejectRequest(
                        claim.LeaseToken,
                        rejected.Diagnostics ?? [PlatformDiagnosticSanitizer.Error("elsa-platform.artifact-rejected", "Recipe artifact apply was rejected.")],
                        outcomes),
                    cancellationToken);
                return;
            }

            var first = outcomes.FirstOrDefault();
            await client.CompleteAsync(
                command.Id,
                new PlatformRuntimeCommandCompleteRequest(
                    claim.LeaseToken,
                    first?.ObservedDigest,
                    first?.RuntimeReference,
                    [PlatformDiagnosticSanitizer.Info("elsa-platform.command-completed", "Recipe deployment command completed.")],
                    outcomes),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Elsa Platform deployment worker failed to apply command {CommandId}.", command.Id);
            await client.FailAsync(
                command.Id,
                new PlatformRuntimeCommandFailRequest(
                    claim.LeaseToken,
                    [PlatformDiagnosticSanitizer.Error("elsa-platform.command-failed", ex.Message)],
                    outcomes),
                cancellationToken);
        }
    }
}
