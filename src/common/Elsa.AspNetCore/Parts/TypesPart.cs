using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Elsa.AspNetCore.Parts;

/// <summary>
/// A custom <see cref="ApplicationPart"/> that controls what types will be provided to the system.
/// Use this to control what controllers should be made available from other libraries. 
/// </summary>
public class TypesPart : ApplicationPart, IApplicationPartTypeProvider
{
    public TypesPart(params Type[] types)
    {
        Types = types.Select(t => t.GetTypeInfo());
    }

    public override string Name => string.Join(", ", Types.Select(t => t.FullName));

    public IEnumerable<TypeInfo> Types { get; }
}