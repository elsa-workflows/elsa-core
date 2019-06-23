using System;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IConnectionBuilder
    {
        Func<IActivityBuilder> Source { get; }
        Func<IActivityBuilder> Target { get; }
        string Outcome { get; }
        Connection BuildConnection();
    }
}