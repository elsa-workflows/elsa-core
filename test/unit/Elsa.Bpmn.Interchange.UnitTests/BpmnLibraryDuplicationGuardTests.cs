using System.Reflection;
using System.Runtime.CompilerServices;
using Bpmn.Interchange;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Features;
using Elsa.Bpmn.Interchange.Features;

namespace Elsa.Bpmn.Interchange.UnitTests;

/// <summary>
/// Guards D12 (see #7909/#7934): no type under the Elsa.Bpmn* assemblies may reimplement BPMN
/// semantics that the Bpmn.* library already provides. This has already happened once, in
/// elsa-foundation, where a parallel BpmnElement/BpmnGraph/etc. semantics core grew alongside the
/// shared library because only the interchange half was migrated. A review habit did not catch
/// that; this test exists to.
/// This guard lives in the interchange test project, not the Elsa.Bpmn one, because it is the only
/// project that sees both Elsa.Bpmn and Elsa.Bpmn.Interchange transitively without inverting the
/// module layering that the guard itself protects.
/// The dependency direction (that Elsa.Bpmn/Elsa.Bpmn.Interchange still reference the Bpmn.*
/// packages rather than dropping them and keeping a local copy) is not asserted at runtime here;
/// it is enforced at compile time by the typeof bindings below, see the comment there.
/// </summary>
public class BpmnLibraryDuplicationGuardTests
{
    // Every type name the assembly defines is checked, not just public ones: an internal
    // reimplementation of BpmnGraph is the same disease as a public one.
    // These typeof bindings are load-bearing beyond just locating the assemblies below: dropping
    // the Bpmn.Semantics package reference from Elsa.Bpmn, or the Bpmn.Interchange package
    // reference from Elsa.Bpmn.Interchange, fails this project's build outright (verified:
    // CS0234), because typeof(BpmnInterpreter) and typeof(BpmnXmlReader) respectively have
    // nowhere else to resolve from. typeof(BpmnDefinitions) pins Bpmn.Model the same way, though
    // Bpmn.Model also arrives transitively via Bpmn.Semantics and Bpmn.Interchange, so dropping
    // only its own package reference from Elsa.Bpmn does not by itself break the build; the
    // binding is kept for symmetry and because BpmnDefinitions itself must still be reachable for
    // the guard's collision check to run. This compile-time enforcement is stronger than a
    // runtime assertion could be where it applies, so no test asserts the dependency direction.
    // Do not remove these bindings or replace them with string-based assembly lookup.
    private static readonly Assembly BpmnModelAssembly = typeof(BpmnDefinitions).Assembly;
    private static readonly Assembly BpmnSemanticsAssembly = typeof(BpmnInterpreter).Assembly;
    private static readonly Assembly BpmnInterchangeAssembly = typeof(BpmnXmlReader).Assembly;
    private static readonly Assembly ElsaBpmnAssembly = typeof(BpmnFeature).Assembly;
    private static readonly Assembly ElsaBpmnInterchangeAssembly = typeof(BpmnInterchangeFeature).Assembly;

    [Fact]
    public void ElsaBpmnAssembly_DoesNotDuplicateBpmnModelOrBpmnSemanticsTypeNames()
    {
        AssertNoTypeNameCollisions(ElsaBpmnAssembly, BpmnModelAssembly, BpmnSemanticsAssembly);
    }

    [Fact]
    public void ElsaBpmnInterchangeAssembly_DoesNotDuplicateBpmnLibraryTypeNames()
    {
        // Bpmn.Interchange owns BpmnXmlReader/BpmnXmlWriter and the import analyzer per the
        // library/module split, so its type names are forbidden here too, alongside Bpmn.Model
        // and Bpmn.Semantics (which Elsa.Bpmn.Interchange can reach transitively).
        AssertNoTypeNameCollisions(ElsaBpmnInterchangeAssembly, BpmnModelAssembly, BpmnSemanticsAssembly, BpmnInterchangeAssembly);
    }

    private static void AssertNoTypeNameCollisions(Assembly elsaAssembly, params Assembly[] libraryAssemblies)
    {
        var libraryTypesByName = libraryAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !IsCompilerGenerated(t))
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.First());

        var collisions = elsaAssembly
            .GetTypes()
            .Where(t => !IsCompilerGenerated(t))
            .Where(t => libraryTypesByName.ContainsKey(t.Name))
            .Select(t => $"{t.FullName} (in {elsaAssembly.GetName().Name}) duplicates {libraryTypesByName[t.Name].FullName} (in {libraryTypesByName[t.Name].Assembly.GetName().Name})")
            .OrderBy(message => message)
            .ToList();

        Assert.True(
            collisions.Count == 0,
            $"{elsaAssembly.GetName().Name} must not reimplement BPMN semantics the library already provides:{Environment.NewLine}{string.Join(Environment.NewLine, collisions)}");
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.Name.StartsWith('<') ||
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
}
