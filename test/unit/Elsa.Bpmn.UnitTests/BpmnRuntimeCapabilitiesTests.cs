using Bpmn.Semantics;
using Elsa.Bpmn.Hosting;

namespace Elsa.Bpmn.UnitTests;

public class BpmnRuntimeCapabilitiesTests
{
    [Fact(DisplayName = "Declared covers every capability the library currently defines")]
    public void Declared_CoversEveryCapabilityTheLibraryDefines()
    {
        Assert.True(
            BpmnRuntimeCapabilities.Declared == BpmnHostCapabilities.Full,
            $"""
             Bpmn.Semantics now defines a capability flag Elsa's runtime host does not declare
             (missing: {BpmnHostCapabilities.Full & ~BpmnRuntimeCapabilities.Declared}).

             This is the one place capability refusal (BpmnGraph.Build's ThrowIfUnmet, and the
             mirroring check in BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied) becomes
             reachable, so it needs a deliberate answer, not a silent gap:

               - If BpmnScopeHost can honour the new flag: implement it, then add the flag to
                 BpmnRuntimeCapabilities.Declared so this test goes green again.
               - If it cannot (yet): leave the flag out of BpmnRuntimeCapabilities.Declared on
                 purpose, so BpmnGraph.Build correctly refuses documents that need it, and update
                 this test's expectation to say so explicitly instead of silencing it.

             Either answer is fine. Declaring a capability Elsa doesn't honour, or leaving this gap
             undocumented, is not.
             """);
    }
}
