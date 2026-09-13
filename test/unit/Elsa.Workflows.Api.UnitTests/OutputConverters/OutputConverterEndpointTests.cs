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
    [Test]
    public async Task Configure_ExposesTheAuthorizedDescriptorRoute()
    {
        var endpoint = new List(Substitute.For<IOutputConverterRegistry>(), SerializationTypeRegistry.CreateDefault());
        var definition = new EndpointDefinition(typeof(List), typeof(EmptyRequest), typeof(ListResponse<OutputConverterDescriptorModel>));

        typeof(List).GetProperty("Definition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.SetValue(endpoint, definition);
        endpoint.Configure();

        await Assert.That(definition.Routes).Contains("/descriptors/output-converters");

        var permission = Elsa.Authorization.EndpointPermissionRegistry.Find(typeof(List));

        await Assert.That(permission.HasValue).IsTrue();
        await Assert.That(permission!.Value.Resource).IsEqualTo(Elsa.Workflows.Api.Permissions.WorkflowPermissions.DescriptorsOutputConverters);
        await Assert.That(permission.Value.Verb).IsEqualTo(Elsa.Authorization.CoreVerbs.View);
    }

    [Test]
    public async Task ListCompatible_FiltersThroughTheRegistryAndExposesOnlySafeDescriptorMetadata()
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
        var model = await Assert.That(response.Items).HasSingleItem();

        await Assert.That(listed).IsTrue();
        await Assert.That(errors).IsEmpty();
        registry.Received(1).FindCompatible(typeof(string), typeof(int));
        await Assert.That(model.Id).IsEqualTo("sample.to-text");
        await Assert.That(model.SourceTypeName).IsEqualTo("String");
        await Assert.That(model.ResultTypeName).IsEqualTo("String");
        await Assert.That(model.DisplayName).IsEqualTo("Convert to text");
        await Assert.That(model.Description).IsEqualTo("Formats the source as text.");
        await Assert.That(model.SettingsSchema!.Value.GetProperty("type").GetString()).IsEqualTo("object");
        await Assert.That(typeof(OutputConverterDescriptorModel).GetProperties())
            .DoesNotContain(property => property.Name is "SourceType" or "ResultType" or "ServiceKey" or "ServiceLifetime");
    }

    [Test]
    [Arguments(null, "String", "The sourceType query parameter is required.")]
    [Arguments("String", null, "The destinationType query parameter is required.")]
    [Arguments("Unsafe.Type", "String", "The sourceType query parameter must be a registered type alias or resolvable safe type name.")]
    [Arguments("String", "Unsafe.Type", "The destinationType query parameter must be a registered type alias or resolvable safe type name.")]
    public async Task TryListCompatible_RejectsMissingOrUnsafeQueryTypes(string? sourceTypeName, string? destinationTypeName, string expectedError)
    {
        var registry = Substitute.For<IOutputConverterRegistry>();
        var endpoint = new List(registry, SerializationTypeRegistry.CreateDefault());

        var listed = endpoint.TryListCompatible(sourceTypeName, destinationTypeName, out var response, out var errors);

        await Assert.That(listed).IsFalse();
        await Assert.That(response.Items).IsEmpty();
        await Assert.That(errors).Contains(expectedError);
        registry.DidNotReceive().FindCompatible(Arg.Any<Type>(), Arg.Any<Type>());
    }
}
