using Elsa.Studio.Contracts;
using Elsa.Studio.PlatformIntegration.Extensions;
using Elsa.Studio.PlatformIntegration.Widgets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.PlatformIntegration.Tests;

public sealed class PlatformIntegrationModuleRegistrationTests
{
    [Fact]
    public void Registers_platform_submission_widgets()
    {
        var services = new ServiceCollection();

        services.AddPlatformIntegrationModule(options =>
        {
            options.PlatformEndpoint = new Uri("https://platform.example.test");
            options.WorkspaceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        });

        var widgetTypes = services
            .Where(x => x.ServiceType == typeof(IWidget))
            .Select(x => x.ImplementationType)
            .ToList();

        Assert.Contains(typeof(WorkflowDefinitionEditorSubmitWidget), widgetTypes);
        Assert.Contains(typeof(WorkflowDefinitionListBulkSubmitWidget), widgetTypes);
        Assert.Contains(typeof(WorkflowDefinitionListRowSubmitWidget), widgetTypes);
    }
}
