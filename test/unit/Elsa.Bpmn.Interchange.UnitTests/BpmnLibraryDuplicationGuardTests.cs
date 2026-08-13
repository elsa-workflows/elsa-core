using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
/// </summary>
public class BpmnLibraryDuplicationGuardTests
{
    // Every type name the assembly defines is checked, not just public ones: an internal
    // reimplementation of BpmnGraph is the same disease as a public one.
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

    [Fact]
    public void ElsaBpmnAssembly_StillDependsOnBpmnModelAndBpmnSemantics()
    {
        // The negative assertions above would be trivially satisfied by dropping the package
        // reference and keeping a local copy instead, so assert the dependency direction too.
        // This module's current scaffolding (BpmnFeature, ModuleExtensions) does not yet call
        // into Bpmn.Model/Bpmn.Semantics, so the compiler prunes them from the IL AssemblyRef
        // table - Assembly.GetReferencedAssemblies() would report them as unreferenced even
        // though the package reference is genuinely there. So this reads the built deps.json
        // instead: a build artifact regenerated on every build (unlike csproj text, which a
        // stale build would not catch), listing the actually-resolved package dependency graph.
        AssertDependsOnPackage(ElsaBpmnAssembly, "Bpmn.Model");
        AssertDependsOnPackage(ElsaBpmnAssembly, "Bpmn.Semantics");
    }

    [Fact]
    public void ElsaBpmnInterchangeAssembly_StillDependsOnBpmnInterchange()
    {
        AssertDependsOnPackage(ElsaBpmnInterchangeAssembly, "Bpmn.Interchange");
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

    private static void AssertDependsOnPackage(Assembly assembly, string expectedPackageName)
    {
        // Assembly.GetName().Name is only null for an assembly loaded from a byte array with no
        // name supplied, which never applies to the on-disk assemblies under test here.
        var assemblyName = assembly.GetName().Name ?? throw new InvalidOperationException($"Assembly at {assembly.Location} has no name.");
        var depsPath = Path.ChangeExtension(assembly.Location, ".deps.json");

        Assert.True(File.Exists(depsPath), $"Expected a deps.json next to {assemblyName} to verify its package dependencies.");

        using var deps = JsonDocument.Parse(File.ReadAllText(depsPath));

        var dependsOnPackage = deps.RootElement
            .GetProperty("targets")
            .EnumerateObject()
            .SelectMany(target => target.Value.EnumerateObject())
            .Where(library => library.Name.StartsWith(assemblyName + "/", StringComparison.Ordinal))
            .Any(library =>
                library.Value.TryGetProperty("dependencies", out var dependencies) &&
                dependencies.EnumerateObject().Any(d => d.Name == expectedPackageName));

        Assert.True(
            dependsOnPackage,
            $"{assemblyName} must depend on {expectedPackageName}, not carry a local copy of its types.");
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.Name.StartsWith('<') ||
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
}
