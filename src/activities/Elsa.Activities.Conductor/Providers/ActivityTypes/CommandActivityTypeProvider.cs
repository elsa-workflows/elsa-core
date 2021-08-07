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
    public class CommandActivityTypeProvider : IActivityTypeProvider
    {
        private readonly IDescribesActivityType _describesActivityType;
        private readonly IActivityActivator _activityActivator;
        private readonly Scoped<IEnumerable<ICommandsProvider>> _scopedCommandsProviders;

        public CommandActivityTypeProvider(IDescribesActivityType describesActivityType, IActivityActivator activityActivator, Scoped<IEnumerable<ICommandsProvider>> scopedCommandsProviders)
        {
            _describesActivityType = describesActivityType;
            _activityActivator = activityActivator;
            _scopedCommandsProviders = scopedCommandsProviders;
        }

        public async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var commands = await GetCommandsAsync(cancellationToken);
            var activityTypes = await GetActivityTypesAsync(commands, cancellationToken).ToListAsync(cancellationToken);
            return activityTypes;
        }

        private async IAsyncEnumerable<ActivityType> GetActivityTypesAsync(IEnumerable<CommandDefinition> commands, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var command in commands)
                yield return await CreateActivityTypeAsync(command, cancellationToken);
        }

        private async Task<ActivityType> CreateActivityTypeAsync(CommandDefinition command, CancellationToken cancellationToken)
        {
            async ValueTask<ActivityDescriptor> CreateDescriptorAsync()
            {
                var des = await _describesActivityType.DescribeAsync<SendCommand>(cancellationToken);

                des.Type = command.Name;
                des.DisplayName = command.DisplayName ?? command.Name;
                des.Description = command.Description;
                des.InputProperties = des.InputProperties.Where(x => x.Name != nameof(SendCommand.CommandName)).ToArray();

                return des;
            }

            var descriptor = await CreateDescriptorAsync();

            return new ActivityType
            {
                Type = typeof(SendCommand),
                TypeName = descriptor.Type,
                DisplayName = descriptor.DisplayName,
                DescribeAsync = CreateDescriptorAsync,
                Description = descriptor.Description,
                ActivateAsync = async context =>
                {
                    var activity = await _activityActivator.ActivateActivityAsync<SendCommand>(context, cancellationToken);
                    activity.CommandName = command.Name;
                    return activity;
                },
                CanExecuteAsync = async (context, instance) => await instance.CanExecuteAsync(context),
                ExecuteAsync = async (context, instance) => await instance.ExecuteAsync(context),
                ResumeAsync = async (context, instance) => await instance.ResumeAsync(context)
            };
        }

        private async Task<IEnumerable<CommandDefinition>> GetCommandsAsync(CancellationToken cancellationToken) =>
            await _scopedCommandsProviders.UseServiceAsync(async commandProviders => await GetCommandsAsync(commandProviders, cancellationToken).ToListAsync(cancellationToken));

        private static async IAsyncEnumerable<CommandDefinition> GetCommandsAsync(IEnumerable<ICommandsProvider> commandProviders, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var commandProvider in commandProviders)
            {
                var commands = await commandProvider.GetCommandsAsync(cancellationToken);

                foreach (var command in commands)
                    yield return command;
            }
        }
    }
}