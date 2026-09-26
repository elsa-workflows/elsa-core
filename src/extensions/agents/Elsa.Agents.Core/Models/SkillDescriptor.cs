using System.ComponentModel;
using System.Reflection;

namespace Elsa.Agents;

/// <summary>
/// A descriptor for a skill.
/// </summary>
public class SkillDescriptor
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Type ClrType { get; set; } = null!;
    
    public SkillDescriptorModel ToModel() => new()
    {
        Name = Name,
        Description = Description,
        ClrTypeName = ClrType.AssemblyQualifiedName!
    };

    public static SkillDescriptor From<TSkill>(string? name = null)
    {
        var clrType = typeof(TSkill);
        var description = clrType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        return new()
        {
            Name = name ?? clrType.Name.Replace("Skill", ""),
            Description = description,
            ClrType = clrType
        };
    }
}