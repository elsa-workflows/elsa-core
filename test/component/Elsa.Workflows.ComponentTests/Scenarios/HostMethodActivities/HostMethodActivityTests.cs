using System.Collections.Immutable;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Elsa.Workflows.ComponentTests.Scenarios.HostMethodActivities;

[ClassDataSource<HostMethodDescriptorFixture>(Shared = SharedType.PerClass)]
public class HostMethodActivityTests(HostMethodDescriptorFixture fixture)
{
    [Test]
    [DisplayName("All TestHostMethod public methods are registered as activities")]
    public async Task AllPublicMethodsRegistered()
    {
        // Arrange
        var allDescriptors = fixture.ListAll();
        var hostMethodDescriptors = allDescriptors
            .Where(d => d.TypeName.StartsWith("Elsa.Dynamic.HostMethod.TestHostMethod."))
            .ToList();

        // Assert - We expect all public methods except ClearLog (which is static) and CustomAttributeMethod (which has custom namespace)
        var expectedMethods = new[]
        {
            "SimpleAction", "GreetPerson", "AddNumbers", "GetMessage", "Calculate",
            "GetAsyncMessage", "UseContext", "ProcessWithCancellation", "CreateBookmark",
            "WithDefaultValue", "AsyncAction", "GetComplexData"
        };

        foreach (var expectedMethod in expectedMethods)
        {
            await Assert.That(hostMethodDescriptors.Any(d => d.Name == expectedMethod)).IsTrue();
        }

        // CustomAttributeMethod has a custom namespace, so check it separately
        var customMethod = allDescriptors.FirstOrDefault(d => d.TypeName == "CustomNamespace.CustomType");
        await Assert.That(customMethod).IsNotNull();
        await Assert.That(customMethod.Name).IsEqualTo("CustomAttributeMethod");

        // Should not include static methods
        await Assert.That(hostMethodDescriptors.All(d => d.Name != "ClearLog")).IsTrue();
    }

    [Test]
    [DisplayName("Method descriptor has correct basic properties (methodName: $methodName, expectedDescription: $expectedDescription)")]
    [Arguments("SimpleAction", "Performs a simple action")]
    [Arguments("GreetPerson", null)]
    [Arguments("GetMessage", null)]
    public async Task DescriptorHasCorrectProperties(string methodName, string? expectedDescription)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor.Name).IsEqualTo(methodName);
        await Assert.That(descriptor.TypeName).IsEqualTo($"Elsa.Dynamic.HostMethod.TestHostMethod.{methodName}");
        await Assert.That(descriptor.Category).IsEqualTo("Test Host Method");

        if (expectedDescription != null)
        {
            await Assert.That(descriptor.Description).IsEqualTo(expectedDescription);
        }
    }

    [Test]
    [DisplayName("Method descriptor has expected input parameters (methodName: $methodName, expectedInputCount: $expectedInputCount)")]
    [Arguments("SimpleAction", 0)]
    [Arguments("GreetPerson", 1)]
    [Arguments("AddNumbers", 2)]
    [Arguments("GetMessage", 1)]
    [Arguments("UseContext", 1)] // ActivityExecutionContext excluded
    [Arguments("ProcessWithCancellation", 1)] // CancellationToken excluded
    [Arguments("AsyncAction", 1)]
    [Arguments("GetComplexData", 2)]
    [Arguments("WithDefaultValue", 1)]
    public async Task DescriptorHasExpectedInputCount(string methodName, int expectedInputCount)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor.Inputs.Count).IsEqualTo(expectedInputCount);
    }

    [Test]
    [DisplayName("Method descriptor has expected output type (methodName: $methodName, expectedOutputType: $expectedOutputType)")]
    [Arguments("SimpleAction", null)] // void
    [Arguments("GetMessage", typeof(string))]
    [Arguments("Calculate", typeof(int))]
    [Arguments("GetAsyncMessage", typeof(string))]
    [Arguments("AsyncAction", null)] // Task (void)
    [Arguments("GetComplexData", typeof(Dictionary<string, object>))]
    [Arguments("CreateBookmark", typeof(string))]
    public async Task DescriptorHasExpectedOutputType(string methodName, Type? expectedOutputType)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        await Assert.That(descriptor).IsNotNull();

        if (expectedOutputType == null)
        {
            await Assert.That(descriptor.Outputs).IsEmpty();
        }
        else
        {
            await Assert.That(descriptor.Outputs).HasSingleItem();
            var output = descriptor.Outputs.First();
            await Assert.That(output.Name).IsEqualTo("Output");
            await Assert.That(output.Type).IsEqualTo(expectedOutputType);
        }
    }

    [Test]
    [DisplayName("Method descriptor has correctly typed input parameters (methodName: $methodName, parameterName: $parameterName, expectedType: $expectedType)")]
    [Arguments("GreetPerson", "name", typeof(string))]
    [Arguments("GetMessage", "prefix", typeof(string))]
    [Arguments("UseContext", "data", typeof(string))]
    [Arguments("ProcessWithCancellation", "item", typeof(string))]
    [Arguments("AsyncAction", "action", typeof(string))]
    [Arguments("WithDefaultValue", "message", typeof(string))]
    public async Task DescriptorHasCorrectInputParameterType(string methodName, string parameterName, Type expectedType)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        await Assert.That(descriptor).IsNotNull();
        var input = descriptor.Inputs.FirstOrDefault(i => i.Name == parameterName);
        await Assert.That(input).IsNotNull();
        await Assert.That(input.Type).IsEqualTo(expectedType);
    }

    [Test]
    [DisplayName("Method with multiple parameters has all parameters correctly defined (methodName: $methodName, parameterNames: [$parameterNames], parameterTypes: [$parameterTypes])")]
    [Arguments("AddNumbers", new[] { "a", "b" }, new[] { typeof(int), typeof(int) })]
    [Arguments("GetComplexData", new[] { "key", "value" }, new[] { typeof(string), typeof(string) })]
    [Arguments("Calculate", new[] { "x", "y" }, new[] { typeof(int), typeof(int) })]
    public async Task DescriptorHasAllParametersCorrectlyDefined(string methodName, string[] parameterNames, Type[] parameterTypes)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor.Inputs.Count).IsEqualTo(parameterNames.Length);

        for (int i = 0; i < parameterNames.Length; i++)
        {
            var input = descriptor.Inputs.FirstOrDefault(p => p.Name == parameterNames[i]);
            await Assert.That(input).IsNotNull();
            await Assert.That(input.Type).IsEqualTo(parameterTypes[i]);
        }
    }

    [Test]
    [DisplayName("Special parameters are excluded from inputs (methodName: $methodName)")]
    [Arguments("UseContext")] // Has ActivityExecutionContext parameter
    [Arguments("ProcessWithCancellation")] // Has CancellationToken parameter
    [Arguments("CreateBookmark")] // Has ActivityExecutionContext parameter
    public async Task SpecialParametersExcludedFromInputs(string methodName)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor.Inputs.All(i => i.Type != typeof(ActivityExecutionContext))).IsTrue();
        await Assert.That(descriptor.Inputs.All(i => i.Type != typeof(CancellationToken))).IsTrue();
    }

    [Test]
    [DisplayName("CustomAttributeMethod uses custom Activity attribute values")]
    public async Task CustomAttributeMethodUsesCustomValues()
    {
        // Act
        var descriptor = fixture.Find("CustomNamespace.CustomType");

        // Assert
        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor.Name).IsEqualTo("CustomAttributeMethod");
        await Assert.That(descriptor.TypeName).IsEqualTo("CustomNamespace.CustomType");
        await Assert.That(descriptor.DisplayName).IsEqualTo("Custom Display Name");
        await Assert.That(descriptor.Description).IsEqualTo("Custom description for this activity");
        await Assert.That(descriptor.Category).IsEqualTo("Custom Category");
    }

    [Test]
    [DisplayName("Async methods are correctly registered (methodName: $methodName, expectedInputs: $expectedInputs, expectedOutputType: $expectedOutputType)")]
    [Arguments("GetAsyncMessage", 1, typeof(string))] // Task<string>
    [Arguments("AsyncAction", 1, null)] // Task (void)
    [Arguments("ProcessWithCancellation", 1, null)] // Task (void)
    public async Task AsyncMethodsCorrectlyRegistered(string methodName, int expectedInputs, Type? expectedOutputType)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor.Inputs.Count).IsEqualTo(expectedInputs);

        if (expectedOutputType == null)
        {
            await Assert.That(descriptor.Outputs).IsEmpty();
        }
        else
        {
            await Assert.That(descriptor.Outputs).HasSingleItem();
            await Assert.That(descriptor.Outputs.First().Type).IsEqualTo(expectedOutputType);
        }
    }
    
    private HostMethodDescriptorSnapshot? FindDescriptor(string methodName) =>
        fixture.Find($"Elsa.Dynamic.HostMethod.TestHostMethod.{methodName}");
}

/// <summary>
/// Materializes the host-method registry once, then tears down the component host before any
/// test body runs. Test bodies consume immutable value snapshots only; no provider, scope, client,
/// DbContext, host, catalog, or other mutable application state crosses test boundaries.
/// </summary>
public sealed class HostMethodDescriptorFixture : IAsyncInitializer
{
    private ImmutableArray<HostMethodDescriptorSnapshot> _descriptors = [];

    [ClassDataSource<Infrastructure>(Shared = SharedType.PerTestSession)]
    public required Infrastructure Infrastructure { get; init; }

    public async Task InitializeAsync()
    {
        var testContext = TestContext.Current
            ?? throw new InvalidOperationException("A current TUnit test context is required to initialize host-method descriptors.");
        var app = new App { Infrastructure = Infrastructure };

        try
        {
            await app.InitializeAsync();
            var cluster = await app.StartAsync(testContext);
            await using var scope = cluster.Pod1.Services.CreateAsyncScope();
            var activityRegistry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();

            _descriptors = activityRegistry.ListAll()
                .Where(IsTestHostMethodDescriptor)
                .Select(HostMethodDescriptorSnapshot.FromDescriptor)
                .ToImmutableArray();
        }
        catch (Exception initializationFailure)
        {
            try
            {
                await app.DisposeAsync();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    "Host-method descriptor initialization and component cleanup both failed.",
                    initializationFailure,
                    cleanupFailure);
            }

            throw;
        }

        await app.DisposeAsync();
    }

    public ImmutableArray<HostMethodDescriptorSnapshot> ListAll() => _descriptors;

    public HostMethodDescriptorSnapshot? Find(string typeName) =>
        _descriptors
            .Where(x => string.Equals(x.TypeName, typeName, StringComparison.Ordinal))
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

    private static bool IsTestHostMethodDescriptor(ActivityDescriptor descriptor) =>
        descriptor.TypeName.StartsWith("Elsa.Dynamic.HostMethod.TestHostMethod.", StringComparison.Ordinal) ||
        string.Equals(descriptor.TypeName, "CustomNamespace.CustomType", StringComparison.Ordinal);
}

public sealed record HostMethodDescriptorSnapshot(
    string Name,
    string TypeName,
    int Version,
    string Category,
    string? DisplayName,
    string? Description,
    ImmutableArray<HostMethodPortSnapshot> Inputs,
    ImmutableArray<HostMethodPortSnapshot> Outputs)
{
    public static HostMethodDescriptorSnapshot FromDescriptor(ActivityDescriptor descriptor) =>
        new(
            descriptor.Name,
            descriptor.TypeName,
            descriptor.Version,
            descriptor.Category,
            descriptor.DisplayName,
            descriptor.Description,
            descriptor.Inputs.Select(x => new HostMethodPortSnapshot(x.Name, x.Type)).ToImmutableArray(),
            descriptor.Outputs.Select(x => new HostMethodPortSnapshot(x.Name, x.Type)).ToImmutableArray());
}

public sealed record HostMethodPortSnapshot(string Name, Type Type);
