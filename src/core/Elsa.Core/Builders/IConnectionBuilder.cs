using System;
using Elsa.Models;

namespace Elsa.Builders
{
    public interface IConnectionBuilder
    {
        Func<IActivityBuilder> Source { get; }
        Func<IActivityBuilder> Target { get; }
        string Outcome { get; }
        ConnectionDefinition BuildConnection();
    }
}