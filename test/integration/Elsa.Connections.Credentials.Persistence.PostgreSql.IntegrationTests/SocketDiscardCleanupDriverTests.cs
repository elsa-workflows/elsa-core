using Elsa.Slack.SocketMode.Persistence;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class SocketDiscardCleanupDriverTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task ActualIneligibleReceiptsCannotPinTheFirstPageAndCompletedScanRestarts()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        for (var i = 0; i < 3; i++)
        {
            var committed = await host.Discards.RecordDiscardAsync(await host.RequestAsync("driver-event-" + i), SocketDiscardTestFixture.Now);
            Assert.Equal(SlackSocketDiscardOutcome.Committed, committed.Outcome);
        }
        var driver = new SlackSocketReceiptCleanupDriver(host.Services.GetRequiredService<IServiceScopeFactory>(), SocketDiscardTestFixture.Authority);
        var beforeHorizon = await RunScanAsync(driver, SocketDiscardTestFixture.Now.AddDays(1), 1, 4);
        Assert.Equal((3, 0), beforeHorizon);
        Assert.Equal(3, (await host.SubscriptionAsync()).RetainedRecords);
        var atHorizon = await RunScanAsync(driver, SocketDiscardTestFixture.Now.AddDays(2), 1, 4);
        Assert.Equal((3, 3), atHorizon);
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        await using var receipts = await host.ReceiptContextAsync();
        Assert.Equal(0, await receipts.Receipts.CountAsync());
        var now = SocketDiscardTestFixture.Now.AddDays(2);
        var request = await host.RequestAsync("driver-later-insertion");
        request = request with { Event = request.Event with { OccurredAt = now } };
        Assert.Equal(SlackSocketDiscardOutcome.Committed, (await host.Discards.RecordDiscardAsync(request, now)).Outcome);
        Assert.Equal((1, 1), await RunScanAsync(driver, now.AddDays(2), 1, 2));
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        Assert.Equal(0, await receipts.Receipts.CountAsync());
    }

    [Fact]
    public async Task ActualDifferentPolicyAuthoritiesKeepIndependentFiniteScans()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var first = await host.Discards.RecordDiscardAsync(await host.RequestAsync("authority-first"), SocketDiscardTestFixture.Now);
        var configuration = AdmissionWorkerHost.Configuration(id: "second-authority-subscription");
        configuration = configuration with { Policy = configuration.Policy with { CleanupAuthority = "second-synthetic-cleanup-authority" } };
        await AdmissionTestLedger.ActivateAsync(host.Admissions, configuration);
        var subscription = (await host.Admissions.FindSubscriptionAsync(configuration.Id))!;
        var request = await host.RequestAsync("authority-second");
        request = request with
        {
            Event = request.Event with { SubscriptionId = subscription.Id },
            ExpectedConfigurationFingerprint = subscription.ConfigurationFingerprint,
            ExpectedActivationEpoch = subscription.ActivationEpoch
        };
        var second = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        Assert.Equal(SlackSocketDiscardOutcome.Committed, first.Outcome);
        Assert.Equal(SlackSocketDiscardOutcome.Committed, second.Outcome);
        var scopes = host.Services.GetRequiredService<IServiceScopeFactory>();
        var firstDriver = new SlackSocketReceiptCleanupDriver(scopes, SocketDiscardTestFixture.Authority);
        var secondDriver = new SlackSocketReceiptCleanupDriver(scopes, configuration.Policy.CleanupAuthority);
        var removed = 0;
        var firstCompleted = false;
        var secondCompleted = false;
        for (var i = 0; i < 3 && (!firstCompleted || !secondCompleted); i++)
        {
            if (!firstCompleted)
            {
                var batch = await firstDriver.RunBatchAsync(1, SocketDiscardTestFixture.Now.AddDays(2));
                removed += batch.Removed;
                firstCompleted = batch.ScanCompleted;
            }
            if (!secondCompleted)
            {
                var batch = await secondDriver.RunBatchAsync(1, SocketDiscardTestFixture.Now.AddDays(2));
                removed += batch.Removed;
                secondCompleted = batch.ScanCompleted;
            }
        }
        Assert.True(firstCompleted);
        Assert.True(secondCompleted);
        Assert.Equal(2, removed);
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        Assert.Equal(0, (await host.Admissions.FindSubscriptionAsync(configuration.Id))!.RetainedRecords);
        Assert.Null(await host.ReceiptAsync(first.ReceiptId!));
        Assert.Null(await host.ReceiptAsync(second.ReceiptId!));
    }

    private static async Task<(int Examined, int Removed)> RunScanAsync(SlackSocketReceiptCleanupDriver driver, DateTimeOffset now, int limit, int maximumBatches)
    {
        var examined = 0;
        var removed = 0;
        for (var i = 0; i < maximumBatches; i++)
        {
            var batch = await driver.RunBatchAsync(limit, now);
            Assert.InRange(batch.Examined, 0, limit);
            Assert.InRange(batch.Removed, 0, batch.Examined);
            examined += batch.Examined;
            removed += batch.Removed;
            if (batch.ScanCompleted)
            {
                return (examined, removed);
            }
        }
        Assert.Fail("Receipt cleanup must complete its finite scan without first-page starvation.");
        return default;
    }
}
