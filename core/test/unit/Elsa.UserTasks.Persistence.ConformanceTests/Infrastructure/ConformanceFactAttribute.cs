using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;

/// <summary>
/// Names the provider a conformance test class runs against. The provider key drives both the coverage
/// report and the skip decision, so a class can never claim coverage it did not exercise.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class ConformanceProviderAttribute(string providerName) : Attribute
{
    public string ProviderName { get; } = providerName;
}

/// <summary>
/// A <see cref="FactAttribute"/> that reports as <em>skipped, with a reason</em> when the declaring class's
/// provider is unreachable, rather than passing vacuously.
///
/// The conformance tests live on shared abstract base classes, so a plain <c>Skip</c> string cannot vary by
/// provider; the discoverers below resolve the provider from the concrete test class instead.
/// </summary>
[XunitTestCaseDiscoverer(
    "Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure.ConformanceFactDiscoverer",
    "Elsa.UserTasks.Persistence.ConformanceTests")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConformanceFactAttribute : FactAttribute;

/// <summary>The <see cref="TheoryAttribute"/> counterpart of <see cref="ConformanceFactAttribute"/>.</summary>
[XunitTestCaseDiscoverer(
    "Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure.ConformanceTheoryDiscoverer",
    "Elsa.UserTasks.Persistence.ConformanceTests")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConformanceTheoryAttribute : TheoryAttribute;

public sealed class ConformanceFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        var display = discoveryOptions.MethodDisplayOrDefault();
        var displayOptions = discoveryOptions.MethodDisplayOptionsOrDefault();

        yield return ConformanceSkip.Resolve(testMethod.TestClass.Class) is { } reason
            ? new XunitSkippedDataRowTestCase(diagnosticMessageSink, display, displayOptions, testMethod, reason)
            : new XunitTestCase(diagnosticMessageSink, display, displayOptions, testMethod);
    }
}

public sealed class ConformanceTheoryDiscoverer(IMessageSink diagnosticMessageSink) : TheoryDiscoverer(diagnosticMessageSink)
{
    public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
    {
        if (ConformanceSkip.Resolve(testMethod.TestClass.Class) is not { } reason)
            return base.Discover(discoveryOptions, testMethod, theoryAttribute);

        // Enumerating the data rows would touch the provider, so an unavailable provider collapses to one
        // skipped case carrying the reason.
        return
        [
            new XunitSkippedDataRowTestCase(
                DiagnosticMessageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testMethod,
                reason)
        ];
    }
}

internal static class ConformanceSkip
{
    /// <summary>Returns the reason this class cannot run, or null when it must.</summary>
    public static string? Resolve(ITypeInfo testClass)
    {
        if (testClass is not IReflectionTypeInfo reflected)
            return null;

        var attribute = reflected.Type.GetCustomAttribute<ConformanceProviderAttribute>(inherit: true);
        // A conformance class with no provider attribute is a wiring mistake. Report it loudly rather than
        // letting it run against an unknown provider and be counted as coverage.
        return attribute is null
            ? $"{reflected.Type.Name} is missing [ConformanceProvider]; the suite cannot tell which provider it covers."
            : ConformanceProviders.Get(attribute.ProviderName).SkipReason;
    }
}
