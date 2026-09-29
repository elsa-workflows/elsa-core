namespace Elsa.Agents;

public class SkillDiscoverer(IEnumerable<ISkillsProvider> providers) : ISkillDiscoverer
{
    public IEnumerable<SkillDescriptor> DiscoverSkills()
    {
        return providers.SelectMany(x => x.GetSkills());
    }
}