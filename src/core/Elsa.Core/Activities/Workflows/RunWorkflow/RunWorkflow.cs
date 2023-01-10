using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Workflows.Helper;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    [Activity(
        Category = "Workflows",
        Description = "Runs a child workflow.",
        Outcomes = new[] { OutcomeNames.Done, "Not Found" }
    )]
    public class RunWorkflow : Activity
    {
        private readonly IStartsWorkflow _startsWorkflow;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowStorageService _workflowStorageService;
        private readonly IWorkflowReviver _workflowReviver;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        
        public RunWorkflow(IStartsWorkflow startsWorkflow, IWorkflowRegistry workflowRegistry, IWorkflowStorageService workflowStorageService, IWorkflowReviver workflowReviver,
            IWorkflowInstanceStore workflowInstanceStore, IWorkflowDefinitionDispatcher workflowDefinitionDispatcher)
        {
            _startsWorkflow = startsWorkflow;
            _workflowRegistry = workflowRegistry;
            _workflowStorageService = workflowStorageService;
            _workflowReviver = workflowReviver;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
        }

        [ActivityInput(
            Label = "Workflow Definition",
            Hint = "The workflow definition ID to run.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? WorkflowDefinitionId { get; set; } = default!;

        [ActivityInput(
            Label = "Tenant ID",
            Hint = "The tenant ID to which the workflow to run belongs.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? TenantId { get; set; } = default!;

        [ActivityInput(Hint = "Optional input to send to the workflow to run.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Input { get; set; }

        [ActivityInput(
            Hint = "Enter one or more potential child workflow outcomes you might want to handle.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json },
            ConsiderValuesAsOutcomes = true
        )]
        public ISet<string> PossibleOutcomes { get; set; } = new HashSet<string>();

        [ActivityInput(
            Label = "Correlation ID",
            Hint = "The correlation ID to associate with the workflow to run.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CorrelationId { get; set; }

        [ActivityInput(
            Label = "Context ID",
            Hint = "The context ID to associate with the workflow to run.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ContextId { get; set; }

        [ActivityInput(
            UIHint = ActivityInputUIHints.MultiLine,
            Hint = "Optional custom attributes to associate with the workflow to run.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json })]
        [Obsolete("Will be removed in future versions.")]
        public Variables? CustomAttributes { get; set; } = default!;

        [ActivityInput(
            Hint = "Fire And Forget: run the child workflow and continue the current one. Blocking: Run the child workflow and suspend the current one until the child workflow finishes.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public RunWorkflowMode Mode { get; set; }

        [ActivityOutput] public FinishedWorkflowModel? Output { get; set; }

        public string ChildWorkflowInstanceId
        {
            get => GetState<string>()!;
            set => SetState(value);
        }

        [ActivityInput(
            Label = "Retry failed workflow",
            Hint = "True to retry existing ChildWorkflow instance instead of creating a new one when faulted.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public bool RetryFailedActivities { get; set; } = default!;
        public Dictionary<string, string> AlreadyExecutedChildren
        {
            get => GetState<Dictionary<string,string>>()!;
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;

            var workflowBlueprint = await FindWorkflowBlueprintAsync(cancellationToken);
            WorkflowStatus? childWorkflowStatus = null;
            WorkflowInstance? childWorkflowInstance = null;

            // Somehow the initial input changes when retrying, so we hash the values
            // when retrying this activity if faulted. If there is only one with this activity id in the workflow, we don't need to use the hash because
            // ChildWorkflowInstanceId will have the id of the sub workflow. But if it has failed in a loop, as ChildWorkflowInstanceId is metadata, it will always
            // have stored just the last workflow instance executed, but not the rest, so we need to discover what is the real workflow instance using hashed input.

            string hash = default!;

            if (RetryFailedActivities)
            {
                AlreadyExecutedChildren ??= new Dictionary<string, string>();
                hash = HashHelper.Hash(context.Input);
            }

            //We know it is a retry if ChildWorkflowInstanceId has a value here, but that value is not the real associated ChildWorkflow
            if (RetryFailedActivities && !string.IsNullOrEmpty(ChildWorkflowInstanceId) && AlreadyExecutedChildren.ContainsKey(hash))
            {
                ChildWorkflowInstanceId = AlreadyExecutedChildren.GetValueOrDefault(hash);
                childWorkflowInstance = await _workflowInstanceStore.FindByIdAsync(ChildWorkflowInstanceId);

                if (childWorkflowInstance == null)
                {
                    throw new Exception();
                }
                switch (childWorkflowInstance.WorkflowStatus)
                {
                    case WorkflowStatus.Idle:
                    case WorkflowStatus.Finished:
                    case WorkflowStatus.Suspended:
                    case WorkflowStatus.Running:
                    case WorkflowStatus.Cancelled:
                        break;
                    case WorkflowStatus.Faulted:
                        await _workflowReviver.ReviveAndRunAsync(childWorkflowInstance, cancellationToken);
                        break;
                    default:
                        break;
                }
                childWorkflowStatus = childWorkflowInstance.WorkflowStatus;
                ChildWorkflowInstanceId = childWorkflowInstance.Id;

                context.JournalData.Add("Workflow Blueprint ID", workflowBlueprint?.Id);
                context.JournalData.Add("Workflow Instance ID", childWorkflowInstance.Id);
                context.JournalData.Add("Workflow Instance Status", childWorkflowInstance.WorkflowStatus);

            }
            else
            {
                if (workflowBlueprint == null || workflowBlueprint.Id == context.WorkflowInstance.DefinitionId)
                    return Outcome("Not Found");

                if (Mode == RunWorkflowMode.DispatchAndForget)
                {
                    var startingActivity = workflowBlueprint.Activities.FirstOrDefault(x => !workflowBlueprint.GetInboundConnections(x.Id).Any());

                    if (startingActivity == null)
                        throw new Exception();
                    
                    await _workflowDefinitionDispatcher.DispatchAsync(
                        new ExecuteWorkflowDefinitionRequest(workflowBlueprint.Id, startingActivity.Id, new WorkflowInput(Input), CorrelationId, ContextId, TenantId), cancellationToken: cancellationToken);
                }
                else
                {
                    var result = await _startsWorkflow.StartWorkflowAsync(workflowBlueprint!, tenantId: TenantId, input: new WorkflowInput(Input), correlationId: CorrelationId, contextId: ContextId, cancellationToken: cancellationToken); ;
                    childWorkflowInstance = result.WorkflowInstance!;
                    childWorkflowStatus = childWorkflowInstance.WorkflowStatus;
                    ChildWorkflowInstanceId = childWorkflowInstance.Id;
                    context.JournalData.Add("Workflow Instance ID", childWorkflowInstance.Id);
                    context.JournalData.Add("Workflow Instance Status", childWorkflowInstance.WorkflowStatus);
                }

                context.JournalData.Add("Workflow Blueprint ID", workflowBlueprint.Id);

                if (RetryFailedActivities)
                    AlreadyExecutedChildren.Add(HashHelper.Hash(context.Input), ChildWorkflowInstanceId);
            }
            
            return Mode switch
            {
                RunWorkflowMode.DispatchAndForget => Done(),
                RunWorkflowMode.FireAndForget => Done(),
                RunWorkflowMode.Blocking when childWorkflowStatus == WorkflowStatus.Finished => await ResumeSynchronouslyAsync(context, childWorkflowInstance, cancellationToken),
                RunWorkflowMode.Blocking when childWorkflowStatus == WorkflowStatus.Suspended => Suspend(),
                RunWorkflowMode.Blocking when childWorkflowStatus == WorkflowStatus.Idle => Suspend(),
                RunWorkflowMode.Blocking when childWorkflowStatus == WorkflowStatus.Faulted => Fault($"Workflow {childWorkflowInstance.Id} faulted"),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode))
            };
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var model = (FinishedWorkflowModel)context.WorkflowExecutionContext.Input!;
            return OnResumeInternal(context, model);
        }

        private async Task<IActivityExecutionResult> ResumeSynchronouslyAsync(ActivityExecutionContext context, WorkflowInstance childWorkflowInstance, CancellationToken cancellationToken)
        {
            var outputReference = childWorkflowInstance.Output;

            var output = outputReference != null
                ? await _workflowStorageService.LoadAsync(outputReference.ProviderName, new WorkflowStorageContext(childWorkflowInstance, outputReference.ActivityId), "Output", cancellationToken)
                : null;

            var model = new FinishedWorkflowModel
            {
                WorkflowOutput = output,
                WorkflowInstanceId = childWorkflowInstance.Id
            };

            context.LogOutputProperty(this, "Output", output);
            context.JournalData.Add("Child Workflow Instance ID", childWorkflowInstance.Id);

            return OnResumeInternal(context, model);
        }

        private IActivityExecutionResult OnResumeInternal(ActivityExecutionContext context, FinishedWorkflowModel output)
        {
            var results = new List<IActivityExecutionResult> { Done() };

            Output = output;

            if (output.WorkflowOutput is FinishOutput finishOutput)
            {
                // Deconstruct FinishOutput.
                Output = new FinishedWorkflowModel
                {
                    WorkflowOutput = finishOutput.Output,
                    WorkflowInstanceId = output.WorkflowInstanceId
                };

                var outcomeNames = finishOutput.Outcomes.Except(new[] { OutcomeNames.Done });
                results.Add(Outcomes(outcomeNames));
            }

            return Combine(results);
        }

        private async Task<IWorkflowBlueprint?> FindWorkflowBlueprintAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(WorkflowDefinitionId))
                return null;

            return await _workflowRegistry.FindAsync(WorkflowDefinitionId, VersionOptions.Published, TenantId, cancellationToken);
        }

        public enum RunWorkflowMode
        {
            /// <summary>
            /// Dispatches the specified workflow and directly continues.
            /// </summary>
            DispatchAndForget,
            
            /// <summary>
            /// Run the specified workflow and continue with the current one. 
            /// </summary>
            FireAndForget,

            /// <summary>
            /// Run the specified workflow and continue once the child workflow finishes. 
            /// </summary>
            Blocking
        }
    }
}