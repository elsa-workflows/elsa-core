using System.Collections.Generic;

namespace Elsa.Services.Models
{
    public interface ICompositeActivityBlueprint : IActivityBlueprint
    {
        public ICollection<IActivityBlueprint> Activities { get; set; }

        public ICollection<IConnection> Connections { get; set; }
        IActivityPropertyProviders ActivityPropertyProviders { get; set; }
    }
}