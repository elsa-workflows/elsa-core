using Elsa.Studio.Bpmn.BpmnJs;
using Elsa.Studio.Bpmn.BpmnJs.Extensions;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Bpmn.Extensions;
using Elsa.Studio.Bpmn.X6;
using Elsa.Studio.Bpmn.X6.Extensions;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Bpmn.Tests;

public class BpmnDesignerResolverTests
{
    private static readonly BpmnDesignerDescriptor First = new("first", "_content/First/first.js");
    private static readonly BpmnDesignerDescriptor Second = new("second", "_content/Second/second.js", "_content/Second/second.css");

    [Fact(DisplayName = "With no designer enabled, nothing is resolved, so the caller keeps the BPMN view it has")]
    public void NoDesigner_ResolvesNothing()
    {
        Assert.Null(Resolver().Resolve());
    }

    [Fact(DisplayName = "With one designer enabled, that one is resolved, whether or not the options name it")]
    public void OneDesigner_IsResolved()
    {
        Assert.Same(First, Resolver(designerId: null, First).Resolve());
        Assert.Same(First, Resolver(designerId: "first", First).Resolve());
    }

    [Fact(DisplayName = "With several designers enabled, the one the options name is resolved")]
    public void SeveralDesigners_TheChosenOneIsResolved()
    {
        Assert.Same(Second, Resolver(designerId: "second", First, Second).Resolve());
    }

    [Fact(DisplayName = "With several designers enabled and none chosen, resolving fails and names them")]
    public void SeveralDesigners_WithoutAChoice_Fail()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Resolver(designerId: null, First, Second).Resolve());

        Assert.Contains("'first', 'second'", exception.Message);
        Assert.Contains(nameof(BpmnDesignerOptions.DesignerId), exception.Message);
    }

    [Theory(DisplayName = "Choosing a designer that is not enabled fails")]
    [InlineData("third", new[] { "first", "second" })]
    [InlineData("second", new[] { "first" })]
    [InlineData("first", new string[0])]
    public void ChoosingADesignerThatIsNotEnabled_Fails(string designerId, string[] enabled)
    {
        var designers = enabled.Select(id => new BpmnDesignerDescriptor(id, $"_content/{id}.js")).ToArray();

        var exception = Assert.Throws<InvalidOperationException>(() => Resolver(designerId, designers).Resolve());

        Assert.Contains($"'{designerId}'", exception.Message);
    }

    [Fact(DisplayName = "Two enabled designers that declare the same id fail, even when one is chosen")]
    public void TwoDesignersWithOneId_Fail()
    {
        var other = new BpmnDesignerDescriptor("first", "_content/Other/first.js");

        var exception = Assert.Throws<InvalidOperationException>(() => Resolver(designerId: "first", First, other).Resolve());

        Assert.Contains("'first'", exception.Message);
    }

    [Fact(DisplayName = "Enabling the same designer twice enables it once")]
    public void EnablingADesignerTwice_EnablesItOnce()
    {
        var services = new ServiceCollection().AddBpmnDesigner(First).AddBpmnDesigner(First);

        using var provider = services.BuildServiceProvider();

        Assert.Single(provider.GetServices<BpmnDesignerDescriptor>());
        Assert.Same(First, provider.GetRequiredService<BpmnDesignerResolver>().Resolve());
    }

    [Fact(DisplayName = "With no designer enabled, the default is resolved, and the options may name it")]
    public void NoDesigner_ResolvesTheDefault()
    {
        Assert.Same(First, ResolverWithDefault(designerId: null, defaultDesigner: First).Resolve());
        Assert.Same(First, ResolverWithDefault(designerId: "first", defaultDesigner: First).Resolve());
    }

    [Fact(DisplayName = "With no designer enabled, choosing one other than the default fails")]
    public void NoDesigner_ChoosingAnotherThanTheDefault_Fails()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ResolverWithDefault(designerId: "second", defaultDesigner: First).Resolve());

        Assert.Contains("'second'", exception.Message);
    }

    [Fact(DisplayName = "A designer the host enables replaces the default, which is never counted as enabled")]
    public void AnEnabledDesigner_ReplacesTheDefault()
    {
        Assert.Same(Second, ResolverWithDefault(designerId: null, defaultDesigner: First, Second).Resolve());
        Assert.Throws<InvalidOperationException>(() => ResolverWithDefault(designerId: "first", defaultDesigner: First, Second).Resolve());
    }

    [Fact(DisplayName = "AddDefaultBpmnDesigner makes its designer the one a host that enables none uses, and the first default registered wins")]
    public async Task AddDefaultBpmnDesigner_IsUsedWhenNoneIsEnabled()
    {
        using var provider = new ServiceCollection().AddDefaultBpmnDesigner(First).AddDefaultBpmnDesigner(Second).BuildServiceProvider();

        foreach (var startupTask in provider.GetServices<IStartupTask>())
        {
            await startupTask.ExecuteAsync();
        }

        Assert.Same(First, provider.GetRequiredService<BpmnDesignerResolver>().Resolve());
    }

    [Fact(DisplayName = "A host that enables a designer uses it rather than the default AddDefaultBpmnDesigner registered")]
    public void AddDefaultBpmnDesigner_GivesWayToAnEnabledDesigner()
    {
        using var provider = new ServiceCollection().AddDefaultBpmnDesigner(First).AddBpmnDesigner(Second).BuildServiceProvider();

        Assert.Same(Second, provider.GetRequiredService<BpmnDesignerResolver>().Resolve());
    }

    [Fact(DisplayName = "AddBpmnJsDesigner enables the bpmn-js designer from its static web assets")]
    public void AddBpmnJsDesigner_EnablesBpmnJs()
    {
        using var provider = new ServiceCollection().AddBpmnJsDesigner().BuildServiceProvider();

        var designer = provider.GetRequiredService<BpmnDesignerResolver>().Resolve();

        Assert.Equal(new BpmnDesignerDescriptor("bpmn-js", "_content/Elsa.Studio.Bpmn.BpmnJs/bpmn-js-designer.js", "_content/Elsa.Studio.Bpmn.BpmnJs/bpmn-js-designer.css"), designer);
        Assert.Equal(BpmnJsDesigner.DesignerId, designer!.DesignerId);
    }

    [Fact(DisplayName = "AddX6BpmnDesigner enables the X6 designer from its static web assets")]
    public void AddX6BpmnDesigner_EnablesX6()
    {
        using var provider = new ServiceCollection().AddX6BpmnDesigner().BuildServiceProvider();

        var designer = provider.GetRequiredService<BpmnDesignerResolver>().Resolve();

        Assert.Equal(new BpmnDesignerDescriptor("x6", "_content/Elsa.Studio.Bpmn.X6/x6-designer.js", "_content/Elsa.Studio.Bpmn.X6/x6-designer.css"), designer);
        Assert.Equal(X6BpmnDesigner.DesignerId, designer!.DesignerId);
    }

    [Fact(DisplayName = "A host whose designer choice is ambiguous fails when its startup tasks run")]
    public async Task AmbiguousChoice_FailsAtStartup()
    {
        using var provider = new ServiceCollection().AddBpmnDesigner(First).AddBpmnDesigner(Second).BuildServiceProvider();

        var startupTasks = provider.GetServices<IStartupTask>().ToList();

        Assert.Single(startupTasks);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await startupTasks[0].ExecuteAsync());
    }

    [Fact(DisplayName = "A host that enables no designer starts")]
    public async Task NoDesigner_Starts()
    {
        using var provider = new ServiceCollection().AddBpmnDesignerResolver().BuildServiceProvider();

        foreach (var startupTask in provider.GetServices<IStartupTask>())
            await startupTask.ExecuteAsync();

        Assert.Null(provider.GetRequiredService<BpmnDesignerResolver>().Resolve());
    }

    private static BpmnDesignerResolver Resolver(string? designerId = null, params BpmnDesignerDescriptor[] designers) =>
        new(designers, Microsoft.Extensions.Options.Options.Create(new BpmnDesignerOptions { DesignerId = designerId }));

    private static BpmnDesignerResolver ResolverWithDefault(string? designerId, BpmnDesignerDescriptor defaultDesigner, params BpmnDesignerDescriptor[] designers) =>
        new(designers, Microsoft.Extensions.Options.Options.Create(new BpmnDesignerOptions { DesignerId = designerId }), new DefaultBpmnDesigner(defaultDesigner));
}
