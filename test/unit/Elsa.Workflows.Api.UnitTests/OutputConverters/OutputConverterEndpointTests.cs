using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Models;
using Elsa.Workflows.Api.Endpoints.OutputConverters.List;
using Elsa.Workflows.Models;
using FastEndpoints;
using NSubstitute;

namespace Elsa.Workflows.Api.UnitTests.OutputConverters;

public class OutputConverterEndpointTests
{
    [Fact]
    public void Configure_ExposesTheAuthorizedDescriptorRoute()
    {
        var endpoint = new List(Substitute.For<IOutputConverterRegistry>(), SerializationTypeRegistry.CreateDefault());
        var definition = new EndpointDefinition(typeof(List), typeof(EmptyRequest), typeof(ListResponse<OutputConverterDescriptorModel>));

        typeof(List).GetProperty("Definition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.SetValue(endpoint, definition);
        endpoint.Configure();

        Assert.Contains("/descriptors/output-converters", definition.Routes);

        var permission = Elsa.Authorization.EndpointPermissionRegistry.Find(typeof(List));

        Assert.True(permission.HasValue);
        Assert.Equal(Elsa.Workflows.Api.Permissions.WorkflowPermissions.DescriptorsOutputConverters, permission!.Value.Resource);
        Assert.Equal(Elsa.Authorization.CoreVerbs.View, permission.Value.Verb);
    }

    [Fact]
    public void ListCompatible_FiltersThroughTheRegistryAndExposesOnlySafeDescriptorMetadata()
    {
        using var document = JsonDocument.Parse("""{"type":"object","properties":{"format":{"type":"string"}}}""");
        var descriptor = new OutputConverterDescriptor(
            "sample.to-text",
            typeof(string),
            typeof(string),
            "Convert to text",
            "Formats the source as text.",
            document.RootElement);
        var registry = Substitute.For<IOutputConverterRegistry>();
        registry.FindCompatible(typeof(string), typeof(int)).Returns([descriptor]);
        var endpoint = new List(registry, SerializationTypeRegistry.CreateDefault());

        var listed = endpoint.TryListCompatible("String", "Int32", out var response, out var errors);
        var model = Assert.Single(response.Items);

        Assert.True(listed);
        Assert.Empty(errors);
        registry.Received(1).FindCompatible(typeof(string), typeof(int));
        Assert.Equal("sample.to-text", model.Id);
        Assert.Equal("String", model.SourceTypeName);
        Assert.Equal("String", model.ResultTypeName);
        Assert.Equal("Convert to text", model.DisplayName);
        Assert.Equal("Formats the source as text.", model.Description);
        Assert.Equal("object", model.SettingsSchema!.Value.GetProperty("type").GetString());
        Assert.DoesNotContain(typeof(OutputConverterDescriptorModel).GetProperties(), property => property.Name is "SourceType" or "ResultType" or "ServiceKey" or "ServiceLifetime");
    }

    [Theory]
    [InlineData(null, "String", "The sourceType query parameter is required.")]
    [InlineData("String", null, "The destinationType query parameter is required.")]
    [InlineData("Unsafe.Type", "String", "The sourceType query parameter must be a registered type alias or resolvable safe type name.")]
    [InlineData("String", "Unsafe.Type", "The destinationType query parameter must be a registered type alias or resolvable safe type name.")]
    public void TryListCompatible_RejectsMissingOrUnsafeQueryTypes(string? sourceTypeName, string? destinationTypeName, string expectedError)
    {
        var registry = Substitute.For<IOutputConverterRegistry>();
        var endpoint = new List(registry, SerializationTypeRegistry.CreateDefault());

        var listed = endpoint.TryListCompatible(sourceTypeName, destinationTypeName, out var response, out var errors);

        Assert.False(listed);
        Assert.Empty(response.Items);
        Assert.Contains(expectedError, errors);
        registry.DidNotReceive().FindCompatible(Arg.Any<Type>(), Arg.Any<Type>());
    }
}
