using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using NetBox.Extensions;

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

        public RunWorkflow(IStartsWorkflow startsWorkflow, IWorkflowRegistry workflowRegistry, IWorkflowStorageService workflowStorageService)
        {
            _startsWorkflow = startsWorkflow;
            _workflowRegistry = workflowRegistry;
            _workflowStorageService = workflowStorageService;
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
            SupportedSyntaxes = new[] { SyntaxNames.Json }
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

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var workflowBlueprint = await FindWorkflowBlueprintAsync(cancellationToken);

            if (workflowBlueprint == null || workflowBlueprint.Id == context.WorkflowInstance.DefinitionId)
                return Outcome("Not Found");

            var result = await _startsWorkflow.StartWorkflowAsync(workflowBlueprint!, TenantId, new WorkflowInput(Input), CorrelationId, ContextId, cancellationToken: cancellationToken);
            var childWorkflowInstance = result.WorkflowInstance!;
            var childWorkflowStatus = childWorkflowInstance.WorkflowStatus;
            ChildWorkflowInstanceId = childWorkflowInstance.Id;

            return Mode switch
            {
                RunWorkflowMode.FireAndForget => Done(),
                RunWorkflowMode.Blocking when childWorkflowStatus == WorkflowStatus.Finished => await ResumeSynchronouslyAsync(childWorkflowInstance, cancellationToken),
                RunWorkflowMode.Blocking when childWorkflowStatus == WorkflowStatus.Suspended => Suspend(),
                RunWorkflowMode.Blocking when childWorkflowStatus == WorkflowStatus.Faulted => Fault($"Workflow {childWorkflowInstance.Id} faulted"),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode))
            };
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var model = (FinishedWorkflowModel) context.WorkflowExecutionContext.Input!;
            return OnResumeInternal(model);
        }

        private async Task<IActivityExecutionResult> ResumeSynchronouslyAsync(WorkflowInstance childWorkflowInstance, CancellationToken cancellationToken)
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

            return OnResumeInternal(model);
        }

        private IActivityExecutionResult OnResumeInternal(FinishedWorkflowModel output)
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
            var query = await _workflowRegistry.ListAsync(cancellationToken);

            query = query.Where(x => x.WithVersion(VersionOptions.Published));

            if (WorkflowDefinitionId != null)
                query = query.Where(x => x.Id == WorkflowDefinitionId);

            if (TenantId != null)
                query = query.Where(x => x.TenantId == TenantId);

            if (CustomAttributes != null)
                foreach (var customAttribute in CustomAttributes.Data)
                    query = query.Where(x => Equals(x.CustomAttributes.Get(customAttribute.Key), customAttribute.Value));

            return query.FirstOrDefault();
        }

        public enum RunWorkflowMode
        {
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