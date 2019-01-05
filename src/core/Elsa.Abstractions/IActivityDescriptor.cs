using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa
{
    public interface IActivityDescriptor
    {
        /// <summary>
        /// A value indicating whether this activity can trigger the execution of the workflow.
        /// </summary>
        bool IsTrigger { get; }
        
        /// <summary>
        /// The type of activity handled by this handler.
        /// </summary>
        Type ActivityType { get; }

        /// <summary>
        /// The category of the activity.
        /// </summary>
        LocalizedString Category { get; }
        
        /// <summary>
        /// The friendly name of the activity.
        /// </summary>
        LocalizedString DisplayText { get; }
        
        /// <summary>
        /// A brief description of the activity. Used by tooling.
        /// </summary>
        LocalizedString Description { get; }

        /// <summary>
        /// Returns the available endpoints that can be triggered.
        /// </summary>
        /// <returns></returns>
        IEnumerable<LocalizedString> GetEndpoints(IActivity activity);
    }
}