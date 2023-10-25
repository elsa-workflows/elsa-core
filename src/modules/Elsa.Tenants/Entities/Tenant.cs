using Elsa.Common.Entities;

namespace Elsa.Tenants.Entities;
public class Tenant : Entity
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = default!;
}
