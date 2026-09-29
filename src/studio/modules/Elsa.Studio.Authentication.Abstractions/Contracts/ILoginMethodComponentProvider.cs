using Elsa.Studio.Authentication.Abstractions.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>Marker implemented by dynamically rendered login method components.</summary>
public interface ILoginMethodComponent : IComponent;

/// <summary>Maps one trusted login-method kind to a local Blazor component.</summary>
public interface ILoginMethodComponentProvider
{
    string Kind { get; }
    Type ComponentType { get; }
}

public interface ILoginMethodComponentRegistry
{
    bool TryResolve(string kind, out ILoginMethodComponentProvider provider);
}
