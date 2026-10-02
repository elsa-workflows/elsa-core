using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultAppBarService : IAppBarService
{
    private readonly ICollection<AppBarElement> _elements = new List<AppBarElement>();

    /// <inheritdoc />
    public event Action? AppBarItemsChanged;

    /// <inheritdoc />
    public IEnumerable<AppBarElement> AppBarElements => _elements.OrderBy(x => x.Order).ToList();

    /// <inheritdoc />
    public IEnumerable<RenderFragment> AppBarComponents => AppBarElements.Select(x => x.Component).ToList();

    /// <inheritdoc />
    public void AddAppBarItem<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : IComponent
    {
        AddComponent<T>();
    }

    /// <inheritdoc />
    public void AddComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(float? order = null) where T : IComponent
    {
        var element = new AppBarElement
        {
            Order = order ?? 0,
            Component = builder => builder.CreateComponent<T>()
        };

        AddElement(element);
    }

    /// <inheritdoc />
    public void AddElement<T>(float? order = null) where T : AppBarElement, new()
    {
        var element = new T();

        if (order.HasValue)
            element.Order = order.Value;

        AddElement(element);
    }

    /// <inheritdoc />
    public void AddElement(AppBarElement element)
    {
        if (_elements.Contains(element))
            return;

        _elements.Add(element);
        AppBarItemsChanged?.Invoke();
    }
}
