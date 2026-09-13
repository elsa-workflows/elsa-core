using System.Reflection;
using Bpmn.Model.State;
using Elsa.Bpmn.Hosting;
using System.Threading.Tasks;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// <see cref="BpmnDiagnosticEventNames"/> mirrors every member of <see cref="BpmnDiagnosticKind"/> by name so
/// <see cref="BpmnScopeHost"/> can derive an execution log event name from the enum without depending on the
/// library's integer values. Nothing in the compiler links the two, so a library upgrade that renames or adds a
/// member would drift silently; these tests catch that at build time instead.
/// </summary>
public class BpmnDiagnosticEventNamesTests
{
    private static readonly IReadOnlyDictionary<string, string> EventNameConstants = typeof(BpmnDiagnosticEventNames)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string) && f.Name != nameof(BpmnDiagnosticEventNames.Source))
        .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);

    [Test]
    public async Task EveryDiagnosticKindMember_HasAnIdenticallyNamedConstant()
    {
        var memberNames = Enum.GetNames<BpmnDiagnosticKind>();

        foreach (var name in memberNames)
        {
            await Assert.That(EventNameConstants.TryGetValue(name, out var value)).IsTrue().Because($"'{name}' has no matching constant in {nameof(BpmnDiagnosticEventNames)}.");
            await Assert.That(value).IsEqualTo(name);
        }
    }

    [Test]
    public async Task NoConstant_IsNotADiagnosticKindMember()
    {
        var memberNames = Enum.GetNames<BpmnDiagnosticKind>().ToHashSet();

        foreach (var name in EventNameConstants.Keys)
            await Assert.That(memberNames).Contains(name);
    }
}
