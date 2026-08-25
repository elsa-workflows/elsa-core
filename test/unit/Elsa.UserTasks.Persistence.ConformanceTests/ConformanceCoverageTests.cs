using System.Text;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Xunit.Abstractions;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

/// <summary>
/// States, in one always-running place, which providers this run actually covered.
///
/// A silently skipped provider reads as covered when it is not, which is how a persistence defect reaches
/// production behind a provider nobody ran. These tests fail when a provider that should have run did not,
/// and otherwise write the full matrix to the test output and to an artifact file.
/// </summary>
public sealed class ConformanceCoverageTests(ITestOutputHelper output)
{
    /// <summary>Providers that must run everywhere, including on a developer machine with no containers.</summary>
    private static readonly string[] RequiredProviders =
    [
        ConformanceProviders.InMemory,
        ConformanceProviders.Sqlite,
        ConformanceProviders.VNext
    ];

    [Fact]
    public void EveryProviderThatMustRunIsReachable()
    {
        var unreachable = RequiredProviders
            .Select(ConformanceProviders.Get)
            .Where(x => !x.IsAvailable)
            .Select(x => $"{x.Name}: {x.SkipReason}")
            .ToList();

        Assert.True(unreachable.Count == 0,
            "These providers must run in every conformance run but did not:" + Environment.NewLine + string.Join(Environment.NewLine, unreachable));
    }

    [Fact]
    public void AProviderRequestedByTheEnvironmentIsNotQuietlyIgnored()
    {
        // Setting the variable to whitespace is the failure mode worth catching: it looks configured on the
        // CI job and gates nothing, so the provider reports as skipped while the operator believes it ran.
        var misconfigured = ConformanceProviders.All
            .Where(x => x.ConnectionStringVariable is not null && !x.IsAvailable)
            .Where(x => Environment.GetEnvironmentVariable(x.ConnectionStringVariable!) is not null)
            .Select(x => $"{x.Name}: {x.ConnectionStringVariable} is set but empty.")
            .ToList();

        Assert.True(misconfigured.Count == 0, string.Join(Environment.NewLine, misconfigured));
    }

    [Fact]
    public void TheCoverageMatrixIsReported()
    {
        var report = BuildReport();
        output.WriteLine(report);

        var path = Path.Combine(AppContext.BaseDirectory, "user-task-conformance-coverage.md");
        File.WriteAllText(path, report);

        Assert.Contains("| Provider |", report, StringComparison.Ordinal);
    }

    private static string BuildReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# User Tasks persistence conformance coverage").AppendLine();
        builder.AppendLine("| Provider | Repository | Guest sessions | Outbox | Status |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var provider in ConformanceProviders.All)
        {
            var status = provider.IsAvailable ? "covered" : $"**not covered** — {provider.SkipReason}";
            builder.AppendLine($"| {provider.Name} | {Mark(provider.Name, Contract.Repository)} | {Mark(provider.Name, Contract.GuestSessions)} | {Mark(provider.Name, Contract.Outbox)} | {status} |");
        }

        builder.AppendLine();
        builder.AppendLine("`yes` means the suite ran against that contract in this run. A blank cell means the provider");
        builder.AppendLine("ships no implementation of it; `no` means it has one that this run did not exercise.");
        return builder.ToString();
    }

    private static string Mark(string providerName, Contract contract)
    {
        // VNext ships a repository only. Saying so beats leaving it to be inferred from an absent class.
        if (providerName == ConformanceProviders.VNext && contract != Contract.Repository)
            return "n/a";
        return ConformanceProviders.Get(providerName).IsAvailable ? "yes" : "no";
    }

    private enum Contract
    {
        Repository,
        GuestSessions,
        Outbox
    }
}
