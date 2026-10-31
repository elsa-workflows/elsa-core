using Elsa.Dashboard.Abstractions.Models;

namespace Elsa.Dashboard.Api.UnitTests;

/// <summary>Which overview sections a caller reads: a section is either readable, with its data, or withheld, without it.</summary>
public record SectionAccess
{
    public static readonly SectionAccess None = new();
    public static readonly SectionAccess All = new() { Runtime = true, Instances = true, StructuredLogs = true, ConsoleLogs = true };

    public bool Runtime { get; init; }
    public bool Instances { get; init; }
    public bool StructuredLogs { get; init; }
    public bool ConsoleLogs { get; init; }

    /// <summary>The ids of the metric cards and panels built from the readable data, where the caller holds only that permission.</summary>
    public string[] Metrics { get; init; } = [];
    public string[] Panels { get; init; } = [];

    /// <summary>What a caller holding exactly one section's permission reads, for contributors supplying <see cref="TestContributor.Declared"/> data.</summary>
    public static readonly IReadOnlyList<(DashboardPermission Permission, SectionAccess Readable)> BySinglePermission =
    [
        (TestContributor.RuntimeView, new() { Runtime = true, Metrics = ["runtime"] }),
        (TestContributor.InstancesView, new() { Instances = true, Metrics = [TestContributor.InstancesMetric], Panels = [TestContributor.InstancesPanel] }),
        (TestContributor.StructuredLogsView, new() { StructuredLogs = true, Panels = [TestContributor.LogsPanel] }),
        (TestContributor.ConsoleLogsView, new() { ConsoleLogs = true })
    ];

    /// <summary>Asserts the overview of a caller against the sections it may read, for contributors supplying <see cref="TestContributor.Declared"/> data.</summary>
    public void AssertOn(DashboardOverview overview)
    {
        AssertSection(Runtime, overview.Runtime.Capability, overview.Runtime.Status == DashboardRuntimeStatusKeys.AcceptingWork);
        AssertSection(Instances, overview.WorkflowInstances.Capability, overview.WorkflowInstances.Running == 3);
        AssertSection(StructuredLogs, overview.Diagnostics.StructuredLogs.Capability, overview.Diagnostics.StructuredLogs.SourceCount == 2);
        AssertSection(ConsoleLogs, overview.Diagnostics.ConsoleLogs.Capability, overview.Diagnostics.ConsoleLogs.SourceCount == 4);
    }

    private static void AssertSection(bool readable, DashboardCapabilityStatus capability, bool hasData)
    {
        Assert.Equal(readable, capability.Status != DashboardCapabilityStatus.Unauthorized.Status);
        Assert.Equal(readable, hasData);
    }
}

internal static class TheoryDataExtensions
{
    public static TheoryData<T1, T2> ToTheoryData<T1, T2>(this IEnumerable<(T1, T2)> rows)
    {
        var data = new TheoryData<T1, T2>();

        foreach (var (first, second) in rows)
        {
            data.Add(first, second);
        }

        return data;
    }
}
