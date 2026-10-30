namespace Elsa.Agents;

public interface ISkillDiscoverer
{
    IEnumerable<SkillDescriptor> DiscoverSkills();
}