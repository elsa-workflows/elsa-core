using Elsa.Persistence.Common.Entities;

namespace Elsa.Identity.Entities;

public class User : Entity
{
    public string Name { get; set; } = default!;
    public string HashedPassword { get; set; } = default!;
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}