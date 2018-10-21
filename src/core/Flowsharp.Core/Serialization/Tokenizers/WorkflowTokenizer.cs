using System;
using System.Collections.Generic;
using System.Linq;
using Flowsharp.Activities;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Flowsharp.Serialization.Tokenizers
{
    public class WorkflowTokenizer : IWorkflowTokenizer
    {
        private readonly ITokenizerInvoker tokenizerInvoker;
        private readonly IEnumerable<IActivityHandler> activityHandlers;

        public WorkflowTokenizer(ITokenizerInvoker tokenizerInvoker, IEnumerable<IActivityHandler> activityHandlers)
        {
            this.tokenizerInvoker = tokenizerInvoker;
            this.activityHandlers = activityHandlers;
        }

        public JToken Tokenize(Workflow value)
        {
            var workflow = value;
            var activityTuples = workflow.Activities.Select((x, i) => new {Activity = x, Id = i + 1}).ToList();
            var context = new WorkflowTokenizationContext
            {
                ActivityIdLookup = activityTuples.ToDictionary(x => x.Activity, x => x.Id),
                ActivityLookup = activityTuples.ToDictionary(x => x.Id, x => x.Activity),
            };
            var scopeIdLookup = workflow.Scopes.Select((x, i) => new {Id = i + 1, Scope = x}).ToDictionary(x => x.Scope, x => x.Id);

            return new JObject
            {
                {"metadata", JToken.FromObject(workflow.Metadata, JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver()}))},
                {"status", workflow.Status.ToString()},
                {"activities", SerializeActivities(context)}, 
                {"connections", SerializeConnections(context, workflow)}, 
                {"haltedActivities", SerializeHaltedActivities(context, workflow)},
                {"scopes", SerializeScopes(context, scopeIdLookup)},
                {"currentScope", SerializeCurrentScope(workflow, scopeIdLookup)}
            };
        }

        public Workflow Detokenize(JToken token)
        {
            var activityDictionary = DeserializeActivities(token);            
            var serializationContext = new WorkflowTokenizationContext
            {
                ActivityIdLookup = activityDictionary.ToDictionary(x => x.Value, x => x.Key),
                ActivityLookup = activityDictionary,
            };
            
            var scopeLookup = DeserializeScopes(token, serializationContext);
            var workflow = new Workflow
            {
                Metadata = token["metadata"].ToObject<WorkflowMetadata>(new JsonSerializer(){ ContractResolver = new CamelCasePropertyNamesContractResolver()}),
                Status = (WorkflowStatus)Enum.Parse(typeof(WorkflowStatus), token["status"].Value<string>()),
                Activities = activityDictionary.Values.ToList(),
                Connections = DeserializeConnections(token, activityDictionary).ToList(),
                BlockingActivities = DeserializeHaltedActivities(token, activityDictionary).ToList(),
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
                        {"name", variable.Key},
                        {"value", variableValueModel}
                    });
                }
                
                scopeModel.Add("id", scopeEntry.Value);
                scopeModel.Add("variables", variableModels);
                scopeModel.Add("lastResult", lastResultModel);
                scopeModels.Add(scopeModel);
            }

            return scopeModels;
        }

        private JArray SerializeHaltedActivities(WorkflowTokenizationContext context, Workflow workflow)
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
                    source = new { activityId = sourceId, endpoint = connection.Source.Name },
                    target = new { activityId = targetId }
                }));
            }

            return connectionModels;
        }

        private JArray SerializeActivities(WorkflowTokenizationContext context)
        {
            var activityModels = new JArray();
            
            foreach (var item in context.ActivityIdLookup)
            {
                var activity = item.Key;
                var activityId = item.Value;
                var activityModel = JObject.FromObject(activity, new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver()});
                
                activityModel.AddFirst(new JProperty("name", activity.GetType().Name));
                activityModel.AddFirst(new JProperty("id", activityId));
                activityModels.Add(activityModel);
            }

            return activityModels;
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

        private IEnumerable<IActivity> DeserializeHaltedActivities(JToken token, IDictionary<int, IActivity> activityDictionary)
        {
            var haltedActivityModels = (JArray) token["haltedActivities"] ?? new JArray();

            foreach (var haltedActivityModel in haltedActivityModels)
            {
                var activityId = haltedActivityModel.Value<int>();
                var activity = activityDictionary[activityId];
                yield return activity;
            }
        }

        private IEnumerable<Connection> DeserializeConnections(JToken token, IDictionary<int, IActivity> activityDictionary)
        {
            var connectionModels = (JArray) token["connections"] ?? new JArray();

            foreach (var connectionModel in connectionModels)
            { 
                var sourceActivityId = connectionModel["source"]["activityId"].Value<int>();
                var sourceEndpointName = connectionModel["source"]["endpoint"].Value<string>();
                var targetActivityId = connectionModel["target"]["activityId"].Value<int>();
                var sourceActivity = activityDictionary[sourceActivityId];
                var targetActivity = activityDictionary[targetActivityId];
                
                yield return new Connection(sourceActivity, sourceEndpointName, targetActivity);
            }
        }

        private IDictionary<int, IActivity> DeserializeActivities(JToken token)
        {
            var activityModels = (JArray)token["activities"] ?? new JArray();
            var dictionary = new Dictionary<int, IActivity>();

            foreach (var activityModel in activityModels)
            {
                var id = activityModel["id"].Value<int>();
                var name = activityModel["name"].Value<string>();
                var activityType = GetActivityType(name);
                var activity = activityType != null ? (IActivity)activityModel.ToObject(activityType) : new UnknownActivity();

                dictionary.Add(id, activity);
            }

            return dictionary;
        }

        private Type GetActivityType(string activityName)
        {
            return activityHandlers.SingleOrDefault(x => x.ActivityType.Name == activityName)?.ActivityType;
        }
    }
}