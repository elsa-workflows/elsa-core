using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Workflows.Api.Options;

/// <summary>
/// Options for the api module.
/// </summary>
public class ApiOptions
{
    /// <summary>
    /// A mode that does not allow editing workflows.
    /// </summary>
    public bool IsReadOnlyMode { get; set; }
}
