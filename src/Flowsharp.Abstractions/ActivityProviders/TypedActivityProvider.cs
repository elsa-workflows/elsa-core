using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Descriptors;
using Flowsharp.Services;

namespace Flowsharp.ActivityProviders
{
    /// <summary>
    /// Provides activities based on <see cref="IActivity"/> implementations that have been registered with the service container.
    /// </summary>
    public class TypedActivityProvider : IActivityProvider
    {
        private readonly Func<IEnumerable<IActivity>> _activitiesFactory;

        public TypedActivityProvider(Func<IEnumerable<IActivity>> activitiesFactory)
        {
            _activitiesFactory = activitiesFactory;
        }

        public Task<IEnumerable<ActivityDescriptor>> GetActivityDescriptorsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_activitiesFactory().Select(ToDescriptor));
        }

        private ActivityDescriptor ToDescriptor(IActivity activity)
        {
            return new ActivityDescriptor
            {
                Name =  activity.Name,
                GetMetadataAsync = activity.ProvideMetadataAsync,
                CanExecuteAsync = activity.CanExecuteAsync,
                GetOutcomes = activity.GetOutcomes,
                ExecuteActivityAsync = activity.ExecuteAsync,
                ResumeActivityAsync = activity.ResumeAsync,
                ReceiveInputAsync = activity.ReceiveInputAsync,
                WorkflowResumedAsync = activity.WorkflowResumedAsync,
                WorkflowResumingAsync = activity.WorkflowResumingAsync,
                WorkflowStartedAsync = activity.WorkflowStartedAsync,
                WorkflowStartingAsync = activity.WorkflowStartingAsync,
                OnActivityExecutedAsync = activity.OnActivityExecutedAsync,
                OnActivityExecutingAsync = activity.OnActivityExecutingAsync
            };
        }
    }
}
