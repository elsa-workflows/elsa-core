using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using static Elsa.Studio.Workflows.Tests.Support.BpmnDocumentFixtures;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// The services the BPMN "Performed by" section and the workflow editor that hosts it need in a bunit test: the real
/// workflows module, with the backend-facing pieces — the activity catalogue, the expression catalogue and the BPMN
/// document endpoints — replaced by in-memory stand-ins.
/// </summary>
internal static class BpmnEditingTestServices
{
    public static readonly ActivityDescriptor HttpRequest = Descriptor("Elsa.HttpRequest", "HTTP Request", Input("Url", "System.Uri", isWrapped: true), Input("Method", "String", isWrapped: true));

    public static readonly ActivityDescriptor Sequence = WithEmbeddedPort(Descriptor("Elsa.Sequence", "Sequence"), container: true);

    public static readonly ActivityDescriptor If = WithEmbeddedPort(Descriptor("Elsa.If", "If"), container: false);

    public static readonly ActivityDescriptor Delay = Descriptor("Elsa.Delay", "Delay", Input("TimeSpan", "TimeSpan", isWrapped: true));

    public static void AddBpmnEditing(this IServiceCollection services, IBpmnInterchangeService documentService)
    {
        services.AddMudServices();
        services.AddLogging();
        services.AddCoreInternal();
        services.AddRemoteBackend();
        services.AddWorkflowsModule();
        services.AddSingleton<ILocalizer, TestLocalizer>();
        services.AddSingleton<IActivityRegistry>(new TestActivityRegistry([WriteLine(), HttpRequest, Sequence, If, Delay]));
        services.AddSingleton<IExpressionService, StubExpressionService>();
        services.AddSingleton(documentService);
    }

    private static ActivityDescriptor WithEmbeddedPort(ActivityDescriptor descriptor, bool container)
    {
        descriptor.IsContainer = container;
        return descriptor with { Ports = [new Port { Name = "Body", Type = PortType.Embedded }] };
    }

    private sealed class StubExpressionService : IExpressionService
    {
        private static readonly ExpressionDescriptor[] Descriptors = [new("Literal", "Literal"), new("JavaScript", "JavaScript")];

        public Task<IEnumerable<ExpressionDescriptor>> ListDescriptorsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExpressionDescriptor>>(Descriptors);
        public Task<ExpressionDescriptor?> GetByTypeAsync(string type, CancellationToken cancellationToken = default) => Task.FromResult(Descriptors.FirstOrDefault(x => x.Type == type));
    }
}
