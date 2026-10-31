using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.HostMethodActivities;

public class HostMethodActivityTests(App app) : AppComponentTest(app)
{
    [Fact(DisplayName = "All TestHostMethod public methods are registered as activities")]
    public void AllPublicMethodsRegistered()
    {
        // Arrange
        var allDescriptors = ActivityRegistry.ListAll().ToList();
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
            Assert.Contains(hostMethodDescriptors, d => d.Name == expectedMethod);
        }

        // CustomAttributeMethod has a custom namespace, so check it separately
        var customMethod = allDescriptors.FirstOrDefault(d => d.TypeName == "CustomNamespace.CustomType");
        Assert.NotNull(customMethod);
        Assert.Equal("CustomAttributeMethod", customMethod.Name);

        // Should not include static methods
        Assert.DoesNotContain(hostMethodDescriptors, d => d.Name == "ClearLog");
    }

    [Theory(DisplayName = "Method descriptor has correct basic properties")]
    [InlineData("SimpleAction", "Performs a simple action")]
    [InlineData("GreetPerson", null)]
    [InlineData("GetMessage", null)]
    public void DescriptorHasCorrectProperties(string methodName, string? expectedDescription)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(methodName, descriptor.Name);
        Assert.Equal($"Elsa.Dynamic.HostMethod.TestHostMethod.{methodName}", descriptor.TypeName);
        Assert.Equal("Test Host Method", descriptor.Category);

        if (expectedDescription != null)
        {
            Assert.Equal(expectedDescription, descriptor.Description);
        }
    }

    [Theory(DisplayName = "Method descriptor has expected input parameters")]
    [InlineData("SimpleAction", 0)]
    [InlineData("GreetPerson", 1)]
    [InlineData("AddNumbers", 2)]
    [InlineData("GetMessage", 1)]
    [InlineData("UseContext", 1)] // ActivityExecutionContext excluded
    [InlineData("ProcessWithCancellation", 1)] // CancellationToken excluded
    [InlineData("AsyncAction", 1)]
    [InlineData("GetComplexData", 2)]
    [InlineData("WithDefaultValue", 1)]
    public void DescriptorHasExpectedInputCount(string methodName, int expectedInputCount)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(expectedInputCount, descriptor.Inputs.Count);
    }

    [Theory(DisplayName = "Method descriptor has expected output type")]
    [InlineData("SimpleAction", null)] // void
    [InlineData("GetMessage", typeof(string))]
    [InlineData("Calculate", typeof(int))]
    [InlineData("GetAsyncMessage", typeof(string))]
    [InlineData("AsyncAction", null)] // Task (void)
    [InlineData("GetComplexData", typeof(Dictionary<string, object>))]
    [InlineData("CreateBookmark", typeof(string))]
    public void DescriptorHasExpectedOutputType(string methodName, Type? expectedOutputType)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        Assert.NotNull(descriptor);

        if (expectedOutputType == null)
        {
            Assert.Empty(descriptor.Outputs);
        }
        else
        {
            Assert.Single(descriptor.Outputs);
            var output = descriptor.Outputs.First();
            Assert.Equal("Output", output.Name);
            Assert.Equal(expectedOutputType, output.Type);
        }
    }

    [Theory(DisplayName = "Method descriptor has correctly typed input parameters")]
    [InlineData("GreetPerson", "name", typeof(string))]
    [InlineData("GetMessage", "prefix", typeof(string))]
    [InlineData("UseContext", "data", typeof(string))]
    [InlineData("ProcessWithCancellation", "item", typeof(string))]
    [InlineData("AsyncAction", "action", typeof(string))]
    [InlineData("WithDefaultValue", "message", typeof(string))]
    public void DescriptorHasCorrectInputParameterType(string methodName, string parameterName, Type expectedType)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        Assert.NotNull(descriptor);
        var input = descriptor.Inputs.FirstOrDefault(i => i.Name == parameterName);
        Assert.NotNull(input);
        Assert.Equal(expectedType, input.Type);
    }

    [Theory(DisplayName = "Method with multiple parameters has all parameters correctly defined")]
    [InlineData("AddNumbers", new[] { "a", "b" }, new[] { typeof(int), typeof(int) })]
    [InlineData("GetComplexData", new[] { "key", "value" }, new[] { typeof(string), typeof(string) })]
    [InlineData("Calculate", new[] { "x", "y" }, new[] { typeof(int), typeof(int) })]
    public void DescriptorHasAllParametersCorrectlyDefined(string methodName, string[] parameterNames, Type[] parameterTypes)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(parameterNames.Length, descriptor.Inputs.Count);

        for (int i = 0; i < parameterNames.Length; i++)
        {
            var input = descriptor.Inputs.FirstOrDefault(p => p.Name == parameterNames[i]);
            Assert.NotNull(input);
            Assert.Equal(parameterTypes[i], input.Type);
        }
    }

    [Theory(DisplayName = "Special parameters are excluded from inputs")]
    [InlineData("UseContext")] // Has ActivityExecutionContext parameter
    [InlineData("ProcessWithCancellation")] // Has CancellationToken parameter
    [InlineData("CreateBookmark")] // Has ActivityExecutionContext parameter
    public void SpecialParametersExcludedFromInputs(string methodName)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        Assert.NotNull(descriptor);
        Assert.DoesNotContain(descriptor.Inputs, i => i.Type == typeof(ActivityExecutionContext));
        Assert.DoesNotContain(descriptor.Inputs, i => i.Type == typeof(CancellationToken));
    }

    [Fact(DisplayName = "CustomAttributeMethod uses custom Activity attribute values")]
    public void CustomAttributeMethodUsesCustomValues()
    {
        // Act
        var descriptor = ActivityRegistry.Find("CustomNamespace.CustomType");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("CustomAttributeMethod", descriptor.Name);
        Assert.Equal("CustomNamespace.CustomType", descriptor.TypeName);
        Assert.Equal("Custom Display Name", descriptor.DisplayName);
        Assert.Equal("Custom description for this activity", descriptor.Description);
        Assert.Equal("Custom Category", descriptor.Category);
    }

    [Theory(DisplayName = "Async methods are correctly registered")]
    [InlineData("GetAsyncMessage", 1, typeof(string))] // Task<string>
    [InlineData("AsyncAction", 1, null)] // Task (void)
    [InlineData("ProcessWithCancellation", 1, null)] // Task (void)
    public void AsyncMethodsCorrectlyRegistered(string methodName, int expectedInputs, Type? expectedOutputType)
    {
        // Act
        var descriptor = FindDescriptor(methodName);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(expectedInputs, descriptor.Inputs.Count);

        if (expectedOutputType == null)
        {
            Assert.Empty(descriptor.Outputs);
        }
        else
        {
            Assert.Single(descriptor.Outputs);
            Assert.Equal(expectedOutputType, descriptor.Outputs.First().Type);
        }
    }
    
    private IActivityRegistry ActivityRegistry => Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
    private ActivityDescriptor? FindDescriptor(string methodName) => ActivityRegistry.Find($"Elsa.Dynamic.HostMethod.TestHostMethod.{methodName}");
}
