using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Services;
using Elsa.Metadata;
using Elsa.Providers.Activities;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Conductor.Providers.ActivityTypes
{
    public class TaskActivityTypeProvider : IActivityTypeProvider
    {
        private readonly IDescribesActivityType _describesActivityType;
        private readonly IActivityActivator _activityActivator;
        private readonly Scoped<IEnumerable<ITasksProvider>> _scopedTasksProviders;

        public TaskActivityTypeProvider(IDescribesActivityType describesActivityType, IActivityActivator activityActivator, Scoped<IEnumerable<ITasksProvider>> scopedTasksProviders)
        {
            _describesActivityType = describesActivityType;
            _activityActivator = activityActivator;
            _scopedTasksProviders = scopedTasksProviders;
        }

        public async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var events = await GetTasksAsync(cancellationToken);
            return await GetActivityTypesAsync(events, cancellationToken).ToListAsync(cancellationToken);
        }

        private async IAsyncEnumerable<ActivityType> GetActivityTypesAsync(IEnumerable<TaskDefinition> events, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var taskDefinition in events)
                yield return await CreateActivityTypeAsync(taskDefinition, cancellationToken);
        }

        private async Task<ActivityType> CreateActivityTypeAsync(TaskDefinition taskDefinition, CancellationToken cancellationToken)
        {
            async ValueTask<ActivityDescriptor> CreateDescriptorAsync()
            {
                var des = await _describesActivityType.DescribeAsync<RunTask>(cancellationToken);

                des.Type = taskDefinition.Name;
                des.DisplayName = taskDefinition.DisplayName ?? taskDefinition.Name;
                des.Description = taskDefinition.Description;
                des.InputProperties = Array.Empty<ActivityInputDescriptor>();
                des.Outcomes = taskDefinition.Outcomes?.ToArray() ?? new[] { OutcomeNames.Done };

                return des;
            }

            var descriptor = await CreateDescriptorAsync();

            return new ActivityType
            {
                Type = typeof(RunTask),
                TypeName = descriptor.Type,
                DisplayName = descriptor.DisplayName,
                DescribeAsync = CreateDescriptorAsync,
                Description = descriptor.Description,
                ActivateAsync = async context =>
                {
                    var activity = await _activityActivator.ActivateActivityAsync<RunTask>(context, cancellationToken);
                    activity.TaskName = taskDefinition.Name;
                    return activity;
                },
                CanExecuteAsync = async (context, instance) => await instance.CanExecuteAsync(context),
                ExecuteAsync = async (context, instance) => await instance.ExecuteAsync(context),
                ResumeAsync = async (context, instance) => await instance.ResumeAsync(context)
            };
        }

        private async Task<IEnumerable<TaskDefinition>> GetTasksAsync(CancellationToken cancellationToken) => 
            await _scopedTasksProviders.UseServiceAsync(async taskProviders => await GetTasksAsync(taskProviders, cancellationToken).ToListAsync(cancellationToken));

        private static async IAsyncEnumerable<TaskDefinition> GetTasksAsync(IEnumerable<ITasksProvider> taskProviders, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var commandProvider in taskProviders)
            {
                var commands = await commandProvider.GetTasksAsync(cancellationToken);

                foreach (var command in commands)
                    yield return command;
            }
        }
    }
}