using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record FindWorkflowByName(string Name, VersionOptions VersionOptions) : IRequest<Workflow?>;