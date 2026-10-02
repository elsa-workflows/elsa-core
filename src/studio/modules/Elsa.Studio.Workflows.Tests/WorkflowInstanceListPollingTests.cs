using System.Reflection;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class WorkflowInstanceListPollingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitialRenderStartsTimerOnlyWhenPollingIsEnabledByDefault(bool pollingEnabled)
    {
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLogging();
        context.Services.AddMudServices();
        context.Services.AddSingleton<ILocalizer>(new TestLocalizer());
        context.Services.AddSingleton<Microsoft.Extensions.Options.IOptions<WorkflowInstanceListPollingOptions>>(Microsoft.Extensions.Options.Options.Create(new WorkflowInstanceListPollingOptions
        {
            IsEnabledByDefault = pollingEnabled,
            IntervalSeconds = 3600
        }));
        context.Services.AddSingleton(DispatchProxy.Create<IUserMessageService, UnusedServiceProxy>());
        context.Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceService, UnusedServiceProxy>());
        context.Services.AddSingleton(DispatchProxy.Create<IWorkflowDefinitionService, WorkflowDefinitionServiceProxy>());
        context.Services.AddSingleton(DispatchProxy.Create<IBackendApiClientProvider, UnusedServiceProxy>());
        context.Services.AddSingleton(DispatchProxy.Create<IFiles, UnusedServiceProxy>());
        context.Services.AddSingleton(DispatchProxy.Create<IDomAccessor, UnusedServiceProxy>());
        context.Services.AddSingleton(DispatchProxy.Create<IRemoteFeatureProvider, RemoteFeatureProviderProxy>());

        var component = context.Render<TestWorkflowInstanceList>();

        Assert.Equal(pollingEnabled, GetTimer(component.Instance) != null);

        await component.Instance.DisposeAsync();
        Assert.Null(GetTimer(component.Instance));
    }

    private static Timer? GetTimer(WorkflowInstanceList component) =>
        (Timer?)typeof(WorkflowInstanceList).GetField("_elapsedTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component);

    private sealed class TestWorkflowInstanceList : WorkflowInstanceList
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private class UnusedServiceProxy : DispatchProxy
    {
        public UnusedServiceProxy()
        {
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected call to {targetMethod!.DeclaringType!.Name}.{targetMethod.Name}.");
    }

    private class RemoteFeatureProviderProxy : DispatchProxy
    {
        public RemoteFeatureProviderProxy()
        {
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == nameof(IRemoteFeatureProvider.IsEnabledAsync)
                ? Task.FromResult(false)
                : throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
    }

    private class WorkflowDefinitionServiceProxy : DispatchProxy
    {
        public WorkflowDefinitionServiceProxy()
        {
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == nameof(IWorkflowDefinitionService.ListAsync)
                ? Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary> { Items = [] })
                : throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string key] => new(key, key);
        public LocalizedString this[string key, params object[] arguments] => new(key, string.Format(key, arguments));
    }
}
