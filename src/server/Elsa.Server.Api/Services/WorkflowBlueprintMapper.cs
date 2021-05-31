using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Server.Api.Mapping;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.Api.Services
{
    public class WorkflowBlueprintMapper : IWorkflowBlueprintMapper
    {
        private readonly IWorkflowBlueprintReflector _workflowBlueprintReflector;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public WorkflowBlueprintMapper(IWorkflowBlueprintReflector workflowBlueprintReflector, IMapper mapper, IServiceScopeFactory serviceScopeFactory)
        {
            _workflowBlueprintReflector = workflowBlueprintReflector;
            _mapper = mapper;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async ValueTask<WorkflowBlueprintModel> MapAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var wrapper = await _workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflowBlueprint, cancellationToken);
            var activityProperties = await Task.WhenAll(wrapper.Activities.Select(async x => (x.ActivityBlueprint.Id, await GetActivityPropertiesAsync(wrapper, x, cancellationToken))));
            var activityPropertyDictionary = activityProperties.ToDictionary(x => x.Id, x => x.Item2);
            return _mapper.Map<WorkflowBlueprintModel>(workflowBlueprint, options => options.Items[ActivityBlueprintConverter.ActivityPropertiesKey] = activityPropertyDictionary);
        }

        private async ValueTask<Variables> GetActivityPropertiesAsync(IWorkflowBlueprintWrapper workflowBlueprintWrapper, IActivityBlueprintWrapper activityBlueprintWrapper, CancellationToken cancellationToken)
        {
            var workflowBlueprint = workflowBlueprintWrapper.WorkflowBlueprint;
            var activityBlueprint = activityBlueprintWrapper.ActivityBlueprint;
            var activityId = activityBlueprint.Id;
            var activityPropertyValueProviders = workflowBlueprint.ActivityPropertyProviders.GetProviders(activityId);
            var activityWrapper = workflowBlueprintWrapper.GetActivity(activityId)!;
            var properties = new Variables();

            if (activityPropertyValueProviders == null) 
                return properties;
            
            foreach (var valueProvider in activityPropertyValueProviders)
            {
                var value = await activityWrapper.EvaluatePropertyValueAsync(valueProvider.Key, cancellationToken);
                properties.Set(valueProvider.Key, value);
            }

            return properties;
        }
    }
}