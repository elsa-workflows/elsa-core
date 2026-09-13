using System.Reflection;
using TUnit.Core.Helpers;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;

/// <summary>
/// Names the provider a conformance test class runs against. The provider key drives the coverage report,
/// skip decision, and test display name, so a class can never claim coverage it did not exercise.
/// </summary>
public sealed class ConformanceProviderAttribute(string providerName) : DisplayNameFormatterAttribute
{
    public string ProviderName { get; } = providerName;

    protected override string FormatDisplayName(DiscoveredTestContext context) =>
        $"{ProviderName}: {context.GetDisplayName()}";
}

/// <summary>Skips every inherited test for an unavailable provider before its shared fixture initializes.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class SkipUnavailableConformanceProviderAttribute() : SkipAttribute("Conformance provider is unavailable")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
        Task.FromResult(ConformanceSkip.Resolve(context.TestDetails.ClassType) is not null);

    protected override string GetSkipReason(TestRegisteredContext context) =>
        ConformanceSkip.Resolve(context.TestDetails.ClassType) ?? Reason;
}

/// <summary>
/// Emits all cursor cases for an available provider and one inert row for an unavailable provider. The
/// class-level skip prevents that inert row from running while preserving the established collapsed count.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConformanceCursorCasesAttribute : DataSourceGeneratorAttribute<string, bool>
{
    protected override IEnumerable<Func<(string, bool)>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        var testClass = TestClassTypeHelper.GetTestClassType(metadata);
        if (testClass is null || ConformanceSkip.Resolve(testClass) is not null)
        {
            yield return () => ("unavailable", false);
            yield break;
        }

        yield return () => ("created", false);
        yield return () => ("created", true);
        yield return () => ("due", false);
        yield return () => ("due", true);
        yield return () => ("priority", false);
        yield return () => ("priority", true);
        yield return () => ("title", false);
        yield return () => ("title", true);
    }
}

internal static class ConformanceSkip
{
    /// <summary>Returns the reason this class cannot run, or null when it must run.</summary>
    public static string? Resolve(Type testClass)
    {
        var attribute = testClass.GetCustomAttribute<ConformanceProviderAttribute>(inherit: true);

        return attribute is null
            ? $"{testClass.Name} is missing [ConformanceProvider]; the suite cannot tell which provider it covers."
            : ConformanceProviders.Get(attribute.ProviderName).SkipReason;
    }
}
