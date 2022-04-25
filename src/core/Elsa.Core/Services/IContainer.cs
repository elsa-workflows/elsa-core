using Elsa.Models;

namespace Elsa.Services;

public interface IContainer : IActivity
{
    ICollection<Variable> Variables { get; }
}