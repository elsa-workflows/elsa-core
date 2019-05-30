using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Extensions;
using Elsa.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization.Tokenizers
{
    public class WorkflowTokenizer : IWorkflowTokenizer
    {
        private readonly ITokenizerInvoker tokenizerInvoker;
        private readonly IActivityLibrary activityLibrary;
        private readonly IClock clock;
        private readonly JsonSerializerSettings serializerSettings;
        private readonly JsonSerializer jsonSerializer;

        public WorkflowTokenizer(ITokenizerInvoker tokenizerInvoker, IActivityLibrary activityLibrary, IClock clock)
        {
            this.tokenizerInvoker = tokenizerInvoker;
            this.activityLibrary = activityLibrary;
            this.clock = clock;

            serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            serializerSettings.Converters.Add(new StringEnumConverter());
            jsonSerializer = JsonSerializer.Create(serializerSettings);
        }

        public async Task<JToken> TokenizeWorkflowAsync(Workflow value, CancellationToken cancellationToken)
        {
            var workflow = value;
            var activityTuples = workflow.Activities.Select(x => new { Activity = x, Id = x.Id }).ToList();
            var context = new WorkflowTokenizationContext
            {
                ActivityIdLookup = activityTuples.ToDictionary(x => x.Activity, x => x.Id),
                ActivityLookup = activityTuples.ToDictionary(x => x.Id, x => x.Activity),
            };
            var scopeIdLookup = workflow.Scopes.Select((x, i) => new { Id = i + 1, Scope = x }).ToDictionary(x => x.Scope, x => x.Id);

            var token = new JObject
            {
                { "id", workflow.Id },
                { "parentId", workflow.ParentId },
                { "metadata", JToken.FromObject(workflow.Metadata, jsonSerializer) },
                { "status", workflow.Status.ToString() },
                { "createdAt", SerializeInstant(workflow.CreatedAt) },
                { "startedAt", SerializeInstant(workflow.StartedAt) },
                { "finishedAt", SerializeInstant(workflow.FinishedAt) },
                { "haltedAt", SerializeInstant(workflow.HaltedAt) },
                { "activities", await SerializeActivitiesAsync(context, cancellationToken) },
                { "connections", SerializeConnections(context, workflow) },
                { "blockingActivities", SerializeBlockingActivities(context, workflow) },
                { "executionLog", SerializeExecutionLog(workflow) },
                { "scopes", SerializeScopes(context, scopeIdLookup) },
                { "currentScope", SerializeCurrentScope(workflow, scopeIdLookup) },
            };

            return token;
        }

        public async Task<Workflow> DetokenizeWorkflowAsync(JToken token, CancellationToken cancellationToken)
        {
            var activityDictionary = await DeserializeActivitiesAsync(token, cancellationToken);
            var serializationContext = new WorkflowTokenizationContext
            {
                ActivityIdLookup = activityDictionary.ToDictionary(x => x.Value, x => x.Key),
                ActivityLookup = activityDictionary,
            };

            var scopeLookup = DeserializeScopes(token, serializationContext);
            var workflow = new Workflow
            {
                Id = token["id"].ToString(),
                ParentId = token["parentId"].ToString(),
                Metadata = token["metadata"].ToObject<WorkflowMetadata>(jsonSerializer),
                Status = (WorkflowStatus) Enum.Parse(typeof(WorkflowStatus), token["status"].Value<string>()),
                CreatedAt = DeserializeInstant(token["createdAt"]) ?? clock.GetCurrentInstant(),
                StartedAt = DeserializeInstant(token["startedAt"]),
                FinishedAt = DeserializeInstant(token["finishedAt"]),
                HaltedAt = DeserializeInstant(token["haltedAt"]),
                Activities = activityDictionary.Values.ToList(),
                Connections = DeserializeConnections(token, activityDictionary).ToList(),
                BlockingActivities = DeserializeBlockingActivities(token, activityDictionary).ToList(),
                ExecutionLog = DeserializeExecutionLog(token).ToList(),
                Scopes = new Stack<WorkflowExecutionScope>(scopeLookup.Values)
            };

            if (!workflow.Scopes.Any())
            {
                workflow.CurrentScope = new WorkflowExecutionScope();
                workflow.Scopes.Push(workflow.CurrentScope);
            }
            else
            {
                var currentScopeId = token["currentScope"].Value<int>();
                workflow.CurrentScope = scopeLookup[currentScopeId];
            }

            return workflow;
        }

        public Task<JToken> TokenizeActivityAsync(IActivity value, CancellationToken cancellationToken)
        {
            var activityModel = JObject.FromObject(value, jsonSerializer);
            return Task.FromResult<JToken>(activityModel);
        }

        public async Task<IActivity> DetokenizeActivityAsync(JToken token, CancellationToken cancellationToken)
        {
            var name = token["name"].Value<string>();
            var descriptor = await GetActivityDescriptorAsync(name, cancellationToken);
            var activityType = descriptor?.ActivityType ?? typeof(UnknownActivity);
            var activity = (IActivity) jsonSerializer.Deserialize(token.CreateReader(), activityType);

            activity.Descriptor = descriptor;
            return activity;
        }

        private JToken SerializeCurrentScope(Workflow workflow, IDictionary<WorkflowExecutionScope, int> scopeIdLookup)
        {
            var scopeId = scopeIdLookup[workflow.CurrentScope];
            return JToken.FromObject(scopeId);
        }

        private JArray SerializeScopes(WorkflowTokenizationContext context, IDictionary<WorkflowExecutionScope, int> scopeIdLookup)
        {
            var scopeModels = new JArray();

            foreach (var scopeEntry in scopeIdLookup)
            {
                var scope = scopeEntry.Key;
                var scopeModel = new JObject();
                var lastResultModel = tokenizerInvoker.Tokenize(context, scope.LastResult);
                var variableModels = new JArray();

                foreach (var variable in scope.Variables)
                {
                    var variableValueModel = tokenizerInvoker.Tokenize(context, variable.Value);

                    variableModels.Add(new JObject
                    {
                        { "name", variable.Key },
                        { "value", variableValueModel }
                    });
                }

                scopeModel.Add("id", scopeEntry.Value);
                scopeModel.Add("variables", variableModels);
                scopeModel.Add("lastResult", lastResultModel);
                scopeModels.Add(scopeModel);
            }

            return scopeModels;
        }

        private JArray SerializeBlockingActivities(WorkflowTokenizationContext context, Workflow workflow)
        {
            var haltedActivityModels = new JArray();

            foreach (var activity in workflow.BlockingActivities)
            {
                var activityId = context.ActivityIdLookup[activity];
                haltedActivityModels.Add(activityId);
            }

            return haltedActivityModels;
        }

        private JArray SerializeConnections(WorkflowTokenizationContext context, Workflow workflow)
        {
            var connectionModels = new JArray();

            foreach (var connection in workflow.Connections)
            {
                var sourceId = context.ActivityIdLookup[connection.Source.Activity];
                var targetId = context.ActivityIdLookup[connection.Target.Activity];

                connectionModels.Add(JObject.FromObject(new
                {
                    source = new { activityId = sourceId, name = connection.Source.Name },
                    target = new { activityId = targetId }
                }));
            }

            return connectionModels;
        }

        private async Task<JArray> SerializeActivitiesAsync(WorkflowTokenizationContext context, CancellationToken cancellationToken)
        {
            var activityModels = new JArray();

            foreach (var item in context.ActivityIdLookup)
            {
                var activity = item.Key;
                var activityModel = await TokenizeActivityAsync(activity, cancellationToken);

                activityModels.Add(activityModel);
            }

            return activityModels;
        }
        
        private JArray SerializeExecutionLog(Workflow workflow)
        {
            var models = new JArray();

            foreach (var entry in workflow.ExecutionLog)
            {
                models.Add(JToken.FromObject(entry, jsonSerializer));
            }

            return models;
        }

        private IDictionary<int, WorkflowExecutionScope> DeserializeScopes(JToken token, WorkflowTokenizationContext context)
        {
            var scopeLookup = new Dictionary<int, WorkflowExecutionScope>();
            var scopeModels = token["scopes"] ?? new JArray();

            foreach (var scopeModel in scopeModels)
            {
                var scope = new WorkflowExecutionScope();
                var scopeId = scopeModel["id"].Value<int>();
                var variableModels = (JArray) scopeModel["variables"];
                var lastResultModel = scopeModel["lastResult"];
                var lastResult = tokenizerInvoker.Detokenize(context, lastResultModel);

                foreach (var variableModel in variableModels)
                {
                    var variableName = variableModel["name"].Value<string>();
                    var variableValueModel = variableModel["value"];
                    var variableValue = tokenizerInvoker.Detokenize(context, variableValueModel);

                    scope.Variables.Add(variableName, variableValue);
                }

                scope.LastResult = lastResult;
                scopeLookup.Add(scopeId, scope);
            }

            return scopeLookup;
        }

        private IEnumerable<IActivity> DeserializeBlockingActivities(JToken token, IDictionary<string, IActivity> activityDictionary)
        {
            var haltedActivityModels = (JArray) token["blockingActivities"] ?? new JArray();

            foreach (var haltedActivityModel in haltedActivityModels)
            {
                var activityId = haltedActivityModel.Value<string>();
                var activity = activityDictionary[activityId];
                yield return activity;
            }
        }
        
        private IEnumerable<LogEntry> DeserializeExecutionLog(JToken token)
        {
            var models = (JArray) token["executionLog"] ?? new JArray();

            return JsonConvert.DeserializeObject<IList<LogEntry>>(models.ToString(), serializerSettings);
        }

        private IEnumerable<Connection> DeserializeConnections(JToken token, IDictionary<string, IActivity> activityDictionary)
        {
            var connectionModels = (JArray) token["connections"] ?? new JArray();

            foreach (var connectionModel in connectionModels)
            {
                var sourceActivityId = connectionModel["source"]["activityId"].Value<string>();
                var sourceEndpointName = connectionModel["source"]["name"].Value<string>();
                var targetActivityId = connectionModel["target"]["activityId"].Value<string>();
                var sourceActivity = activityDictionary[sourceActivityId];
                var targetActivity = activityDictionary[targetActivityId];

                yield return new Connection(sourceActivity, targetActivity, sourceEndpointName);
            }
        }

        private async Task<IDictionary<string, IActivity>> DeserializeActivitiesAsync(JToken token, CancellationToken cancellationToken)
        {
            var activityModels = (JArray) token["activities"] ?? new JArray();
            var dictionary = new Dictionary<string, IActivity>();

            foreach (var activityModel in activityModels)
            {
                var activity = await DetokenizeActivityAsync(activityModel, cancellationToken);
                dictionary.Add(activity.Id, activity);
            }

            return dictionary;
        }
        
        private Instant? DeserializeInstant(JToken token)
        {
            return token?.ToObject<DateTime?>(jsonSerializer)?.ToInstant();
        }
        
        private JToken SerializeInstant(Instant? value)
        {
            return value != null ? JToken.FromObject(value.Value, jsonSerializer) : null;
        }

        private Task<ActivityDescriptor> GetActivityDescriptorAsync(string activityName, CancellationToken cancellationToken)
        {
            return activityLibrary.GetByNameAsync(activityName, cancellationToken);
        }
    }
}