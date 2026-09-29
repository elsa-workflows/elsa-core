namespace Elsa.Agents;

/// <summary>
/// A descriptor of a skill.
/// </summary>
public class SkillDescriptorModel
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string ClrTypeName { get; set; } = null!;
}