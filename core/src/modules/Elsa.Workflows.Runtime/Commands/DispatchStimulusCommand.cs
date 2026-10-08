using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Commands;

public record DispatchStimulusCommand(DispatchStimulusRequest Request) : ICommand<Unit>;