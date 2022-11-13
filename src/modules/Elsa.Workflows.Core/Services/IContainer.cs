using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IContainer : IActivity
{
    ICollection<Variable> Variables { get; }
}