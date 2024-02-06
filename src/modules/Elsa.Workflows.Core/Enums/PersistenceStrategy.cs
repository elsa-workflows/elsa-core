using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Workflows.Enums;
/// <summary>
/// Define the Persistence strategy to store information
/// </summary>
public enum PersistenceStrategy
{
    /// <summary>
    /// Persist using the Parent strategy
    /// </summary>
    Default,

    /// <summary>
    /// Include property to store
    /// </summary>
    Include,

    /// <summary>
    /// Exclude Property to store
    /// </summary>
    Exclude

}
