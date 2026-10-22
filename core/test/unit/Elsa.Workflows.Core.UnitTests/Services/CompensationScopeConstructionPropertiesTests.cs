using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class CompensationScopeConstructionPropertiesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisteredScopeDescriptorIncludesNativeDraftConstructionDefaults(bool useShellFeature)
    {
        using var serviceProvider = CreateWorkflowServices(useShellFeature);
        var registry = serviceProvider.GetRequiredService<IActivityRegistry>();

        await registry.RegisterAsync(typeof(CompensationScope), CancellationToken.None);

        var descriptor = Assert.IsType<ActivityDescriptor>(registry.Find(CompensationScope.ActivityTypeName, 1));
        AssertNativeConstructionProperties(descriptor);

        registry.Clear();
        var unsupportedVersion = CreateDescriptor(CompensationScope.ActivityTypeName, typeof(CompensationScope), 2);
        registry.Register(unsupportedVersion);
        Assert.Empty(unsupportedVersion.ConstructionProperties);

        registry.Clear();
        var differentClrType = CreateDescriptor(CompensationScope.ActivityTypeName, typeof(string), 1);
        registry.Register(differentClrType);
        Assert.Empty(differentClrType.ConstructionProperties);

        registry.Clear();
        var differentTypeName = CreateDescriptor("Elsa.OtherScope", typeof(CompensationScope), 1);
        registry.Register(differentTypeName);
        Assert.Empty(differentTypeName.ConstructionProperties);
    }

    private static ServiceProvider CreateWorkflowServices(bool useShellFeature)
    {
        var services = new ServiceCollection();
        if (useShellFeature)
        {
            var tenantAccessor = Substitute.For<ITenantAccessor>();
            tenantAccessor.TenantId.Returns("test-tenant");
            services.AddSingleton<ITenantAccessor>(tenantAccessor);
            new Elsa.Workflows.ShellFeatures.WorkflowsFeature().ConfigureServices(services);
        }
        else
        {
            var module = services.CreateModule();
            module.UseWorkflows();
            module.Apply();
        }

        return services.BuildServiceProvider();
    }

    private static ActivityDescriptor CreateDescriptor(string typeName, Type clrType, int version) => new()
    {
        TypeName = typeName,
        ClrType = clrType,
        Namespace = "Elsa",
        Name = "Scope",
        Category = "Flow",
        Version = version
    };

    private static void AssertNativeConstructionProperties(ActivityDescriptor descriptor)
    {
        var constructionProperties = descriptor.ConstructionProperties;
        Assert.Equal(3, constructionProperties.Count);
        Assert.Equal(CompensationScope.CurrentDraftSchemaVersion,
            constructionProperties[nameof(CompensationScope.DraftSchemaVersion)]);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<CompensationStep>>(
            constructionProperties[nameof(CompensationScope.Steps)]));
        Assert.Equal(CompensationFaultHandoff.Compensated,
            constructionProperties[nameof(CompensationScope.OnForwardFault)]);

        var jsonOptions = new JsonSerializerOptions { DictionaryKeyPolicy = JsonNamingPolicy.CamelCase };
        var json = Assert.IsType<System.Text.Json.Nodes.JsonObject>(
            JsonSerializer.SerializeToNode(constructionProperties, jsonOptions));
        Assert.Equal(3, json.Count);
        Assert.Equal(1, json["draftSchemaVersion"]!.GetValue<int>());
        Assert.Empty(json["steps"]!.AsArray());
        Assert.Equal(CompensationFaultHandoff.Compensated, json["onForwardFault"]!.GetValue<string>());
    }
}
