using Microsoft.Extensions.Options;

namespace Elsa.Studio.Bpmn.Designers;

/// <summary>Picks the BPMN designer a host uses from the designers it enabled.</summary>
/// <remarks>
/// With no designer enabled, <see cref="Resolve"/> returns the <see cref="DefaultBpmnDesigner"/> when a Studio feature
/// registered one, and otherwise null, in which case the caller keeps the BPMN view it has. With one, that
/// one is used. With several, <see cref="BpmnDesignerOptions.DesignerId"/> must name one of them. A choice that names no
/// enabled designer, or two designers that declare the same id, is a configuration error: <see cref="Resolve"/> throws,
/// and the startup task registered with the designers makes the host fail when it starts rather than when a BPMN
/// workflow is first opened.
/// </remarks>
public sealed class BpmnDesignerResolver(
    IEnumerable<BpmnDesignerDescriptor> designers,
    IOptions<BpmnDesignerOptions> options,
    DefaultBpmnDesigner? defaultDesigner)
{
    /// <summary>Creates a resolver with no designer to fall back to when the host enables none.</summary>
    public BpmnDesignerResolver(IEnumerable<BpmnDesignerDescriptor> designers, IOptions<BpmnDesignerOptions> options)
        : this(designers, options, null)
    {
    }

    private readonly Lazy<BpmnDesignerDescriptor?> _designer = new(() => Choose(designers.ToList(), options.Value.DesignerId, defaultDesigner?.Designer));

    /// <summary>The designer to use, or null when the host enabled none and there is no default to fall back to.</summary>
    /// <exception cref="InvalidOperationException">The enabled designers and <see cref="BpmnDesignerOptions.DesignerId"/> do not determine one designer.</exception>
    public BpmnDesignerDescriptor? Resolve() => _designer.Value;

    private static BpmnDesignerDescriptor? Choose(IReadOnlyList<BpmnDesignerDescriptor> designers, string? designerId, BpmnDesignerDescriptor? defaultDesigner)
    {
        var repeatedIds = designers.GroupBy(designer => designer.DesignerId, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        if (repeatedIds.Count > 0)
            throw new InvalidOperationException($"More than one enabled BPMN designer declares the id {string.Join(", ", repeatedIds.Select(id => $"'{id}'"))}. Enable each designer once.");

        if (designers.Count == 0)
        {
            // The default stands in for an enabled designer, so the options may name it as they may name an enabled one.
            if (designerId is not null && !string.Equals(designerId, defaultDesigner?.DesignerId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{nameof(BpmnDesignerOptions)}.{nameof(BpmnDesignerOptions.DesignerId)} is '{designerId}', but no BPMN designer is enabled.");
            }

            return defaultDesigner;
        }

        if (designerId is null)
        {
            if (designers.Count == 1)
                return designers[0];

            throw new InvalidOperationException($"Several BPMN designers are enabled ({Describe(designers)}). Set {nameof(BpmnDesignerOptions)}.{nameof(BpmnDesignerOptions.DesignerId)} to the one to use.");
        }

        return designers.FirstOrDefault(designer => string.Equals(designer.DesignerId, designerId, StringComparison.Ordinal))
               ?? throw new InvalidOperationException($"{nameof(BpmnDesignerOptions)}.{nameof(BpmnDesignerOptions.DesignerId)} is '{designerId}', but the enabled BPMN designers are {Describe(designers)}.");
    }

    private static string Describe(IEnumerable<BpmnDesignerDescriptor> designers) => string.Join(", ", designers.Select(designer => $"'{designer.DesignerId}'"));
}
