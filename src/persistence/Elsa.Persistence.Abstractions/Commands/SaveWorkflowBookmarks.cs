using System.Collections.Generic;
using System.Linq;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to persist the specified <see cref="WorkflowBookmarks"/> to storage.
/// </summary>
public record SaveWorkflowBookmarks : ICommand
{
    public SaveWorkflowBookmarks(IEnumerable<WorkflowBookmark> workflowBookmarks) => WorkflowBookmarks = workflowBookmarks.ToList();
    public IReadOnlyCollection<WorkflowBookmark> WorkflowBookmarks { get; }
}