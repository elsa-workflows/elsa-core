namespace Elsa.Agents;

/// <summary>
/// Implementations of this interface are responsible for providing skills.
/// </summary>
public interface ISkillsProvider
{
    IEnumerable<SkillDescriptor> GetSkills();
}