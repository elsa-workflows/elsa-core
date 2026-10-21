using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Elsa.Workflows.Runtime.UnitTests")]
[assembly: InternalsVisibleTo("Elsa.Persistence.EFCore.UnitTests")]

// A BPMN call activity is a waiting DispatchWorkflow, and the BPMN host reads how its child ended to route a failed call.
[assembly: InternalsVisibleTo("Elsa.Bpmn")]
