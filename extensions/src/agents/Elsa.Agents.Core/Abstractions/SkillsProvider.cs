namespace Elsa.Agents;

public abstract class SkillsProvider : ISkillsProvider
{
    public virtual IEnumerable<SkillDescriptor> GetSkills() => [];
}