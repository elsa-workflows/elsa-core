using System.Collections.Generic;
using Elsa.Common.Models;
using Elsa.Mediator.Services;

namespace Elsa.Workflows.Runtime.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public record DispatchWorkflowDefinition(
    string DefinitionId, 
    VersionOptions VersionOptions, 
    IDictionary<string, object>? Input = default, 
    string? CorrelationId = default) : ICommand;

// ReSharper disable once UnusedType.Global