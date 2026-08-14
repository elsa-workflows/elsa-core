using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;

/// <summary>
/// The application the binder and the <c>elsa:</c> extension format are exercised in.
/// </summary>
/// <remarks>
/// A real container rather than substitutes: both halves go through Elsa's configured activity serializer and its
/// activity registry, and a stand-in for either would agree with whatever the test expected instead of with Elsa.
/// </remarks>
public abstract class BpmnBindingTestBase : IAsyncLifetime
{
    private readonly IServiceProvider _services;

    protected BpmnBindingTestBase(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();

        Binder = _services.GetRequiredService<BpmnWorkBinder>();
        Format = _services.GetRequiredService<BpmnActivityBindingFormat>();
    }

    protected BpmnWorkBinder Binder { get; }

    protected BpmnActivityBindingFormat Format { get; }

    public Task InitializeAsync() => _services.PopulateRegistriesAsync();

    public Task DisposeAsync() => Task.CompletedTask;

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
