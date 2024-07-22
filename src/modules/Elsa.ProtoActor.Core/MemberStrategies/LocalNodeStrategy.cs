using System.Collections.Immutable;
using Proto.Cluster;

namespace Elsa.ProtoActor.MemberStrategies;

/// Performs placement on current node.
public class LocalNodeStrategy(Cluster cluster) : IMemberStrategy
{
    private Member? _me;
    private ImmutableList<Member> _members = ImmutableList<Member>.Empty;

    public ImmutableList<Member> GetAllMembers()
    {
        return _members;
    }

    public void AddMember(Member member)
    {
        // Avoid adding the same member twice
        if (_members.Any(x => x.Address == member.Address || x.Id == member.Id))
            return;

        if (member.Address.Equals(cluster.System.Address, StringComparison.InvariantCulture))
            _me = member;

        _members = _members.Add(member);
    }

    public void RemoveMember(Member member)
    {
        _members = _members.RemoveAll(x => x.Address == member.Address || x.Id == member.Id);
    }

    public Member? GetActivator(string senderAddress)
    {
        return _me;
    }
}