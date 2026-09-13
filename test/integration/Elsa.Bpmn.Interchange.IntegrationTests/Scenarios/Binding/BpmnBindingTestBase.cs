using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;

/// <summary>
/// The application the binder and the <c>elsa:</c> extension format are exercised in.
/// </summary>
/// <remarks>
/// A real container rather than substitutes: both halves go through Elsa's configured activity serializer and its
/// activity registry, and a stand-in for either would agree with whatever the test expected instead of with Elsa.
/// </remarks>
public abstract class BpmnBindingTestBase
{
    private readonly IServiceProvider _services;

    protected BpmnBindingTestBase()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();

        Binder = _services.GetRequiredService<BpmnWorkBinder>();
        Format = _services.GetRequiredService<BpmnActivityBindingFormat>();
    }

    protected BpmnWorkBinder Binder { get; }

    protected BpmnActivityBindingFormat Format { get; }

    /// <summary>The application the binder is exercised in, for a test that needs to run a bound scope end to end.</summary>
    protected IServiceProvider Services => _services;

    [Before(HookType.Test)]
    public Task InitializeAsync() => _services.PopulateRegistriesAsync();

    [After(HookType.Test)]
    public async Task DisposeAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    /// <summary>Elsa's own identity graph over the bound scope, which is what a published workflow is built from.</summary>
    protected async Task<IReadOnlyList<ActivityNode>> IdentityGraphOfAsync(IActivity root) =>
        (await _services.GetRequiredService<IActivityVisitor>().VisitAsync(root)).Flatten().ToList();

    /// <summary>The binding ref the reader assigns to an element, using its default prefix.</summary>
    protected static string Ref(string elementId) => $"{BpmnXmlReader.DefaultBindingRefPrefix}-{elementId}";

    /// <summary>An element carrying an authored activity binding.</summary>
    protected BpmnElement BoundElement(string elementId, string elementType, IActivity activity) =>
        new(elementId, elementType, bindingRef: Ref(elementId), extensions: BpmnActivityBindingFormat.Attach(null, Format.Write(activity)));

    /// <summary>The literal or expression value an activity input carries.</summary>
    protected static T ValueOf<T>(Input input) => (T)input.Expression!.Value!;
}
