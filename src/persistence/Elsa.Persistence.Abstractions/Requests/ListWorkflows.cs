using System.Collections.Generic;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

/// <summary>
/// A forward-cursor only request for listing workflows. No need to count total results.
/// </summary>
public record ListWorkflows(VersionOptions? VersionOptions = default, int Skip = 0, int Take = 50) : IRequest<IEnumerable<Workflow>>;