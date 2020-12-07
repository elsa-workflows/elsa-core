using System.Collections.Generic;

namespace Elsa.Services.Models
{
    public interface ICompositeActivityBlueprint : IActivityBlueprint
    {
        ICollection<IActivityBlueprint> Activities { get; set; }

        ICollection<IConnection> Connections { get; set; }
        IActivityPropertyProviders ActivityPropertyProviders { get; set; }
    }
}