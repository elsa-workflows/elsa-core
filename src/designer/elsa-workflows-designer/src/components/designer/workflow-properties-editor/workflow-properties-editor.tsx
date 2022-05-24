import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import WorkflowEditorTunnel from '../state';
import {
  TabChangedArgs, TabDefinition, WorkflowDefinition
} from '../../../models';
import {FormEntry} from "../../shared/forms/form-entry";
import {InfoList} from "../../shared/forms/info-list";
import {isNullOrWhitespace} from "../../../utils";

export interface WorkflowPropsUpdatedArgs {
  workflowDefinition: WorkflowDefinition;
}

export interface WorkflowLabelsUpdatedArgs {
  workflowDefinition: WorkflowDefinition;
  labelIds: Array<string>;
}

@Component({
  tag: 'elsa-workflow-properties-editor',
})
export class WorkflowPropertiesEditor {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;

  @Prop({mutable: true}) workflowDefinition?: WorkflowDefinition;
  @Prop() assignedLabelIds: Array<string> = [];
  @Event() workflowPropsUpdated: EventEmitter<WorkflowPropsUpdatedArgs>;
  @Event({bubbles: false}) workflowLabelsUpdated: EventEmitter<WorkflowLabelsUpdatedArgs>;
  @State() private selectedTabIndex: number = 0;

  @Method()
  public async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  public async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  public render() {
    const title = 'Workflow';

    const propertiesTab: TabDefinition = {
      displayText: 'Properties',
      content: () => this.renderPropertiesTab()
    };

    const variablesTab: TabDefinition = {
      displayText: 'Variables',
      content: () => this.renderVariablesTab()
    };

    const tabs = [propertiesTab, variablesTab];

    return (
      <elsa-form-panel
        headerText={title} tabs={tabs} selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}/>
    );
  }

  private renderPropertiesTab = () => {
    const workflow = this.workflowDefinition;
    const assignedLabelIds = this.assignedLabelIds;

    const workflowDetails = {
      'Definition ID': isNullOrWhitespace(workflow.definitionId) ? '(new)' : workflow.definitionId,
      'Version ID': isNullOrWhitespace(workflow.id) ? '(new)' : workflow.id,
      'Version': workflow.version,
      'Status': workflow.isPublished ? 'Published' : 'Draft'
    };

    return <div>
      <FormEntry label="Name" fieldId="workflowName" hint="The name of the workflow.">
        <input type="text" name="workflowName" id="workflowName" value={workflow.name} onChange={e => this.onPropertyEditorChanged(wf => wf.name = (e.target as HTMLInputElement).value)}/>
      </FormEntry>
      <FormEntry label="Description" fieldId="workflowDescription" hint="A brief description about the workflow.">
        <textarea name="workflowDescription" id="workflowDescription" value={workflow.description} rows={6} onChange={e => this.onPropertyEditorChanged(wf => wf.description = (e.target as HTMLTextAreaElement).value)}/>
      </FormEntry>
      <FormEntry label="Labels" fieldId="workflowLabels" hint="Labels allow you to tag the workflow that can be used to query workflows with.">
        <elsa-label-picker onSelectedLabelsChanged={this.onSelectedLabelsChanged} selectedLabels={assignedLabelIds}/>
      </FormEntry>
      <InfoList title="Information" dictionary={workflowDetails}/>
    </div>
  };

  private renderVariablesTab = () => {
    return <div>
      TODO: Variables editor
    </div>
  };

  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex;

  private onPropertyEditorChanged = (apply: (w: WorkflowDefinition) => void) => {
    const workflowDefinition = this.workflowDefinition;
    apply(workflowDefinition);
    return this.workflowPropsUpdated.emit({workflowDefinition});
  }

  private onSelectedLabelsChanged = (e: CustomEvent<Array<string>>) => {
    this.workflowLabelsUpdated.emit({workflowDefinition: this.workflowDefinition, labelIds: e.detail});
  }
}

WorkflowEditorTunnel.injectProps(WorkflowPropertiesEditor, ['activityDescriptors']);
