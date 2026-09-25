using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.WorkflowContexts.Models;

public class ActivityWorkflowContextSettings
{
    /// <summary>
    /// Whether to load the context before executing the activity.
    /// </summary>
    public bool Load { get; set; }

    /// <summary>
    /// Whether to save the context after executing the activity.
    /// </summary>
    public bool Save { get; set; }
}
