import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import WorkflowEditorTunnel from '../state';
import {TabChangedArgs, TabDefinition, WorkflowDefinition
} from '../../../models';
import {FormEntry} from "../../shared/forms/form-entry";
import {InfoList} from "../../shared/forms/info-list";

export interface WorkflowPropsUpdatedArgs {
  workflow: WorkflowDefinition;
}

@Component({
  tag: 'elsa-workflow-properties-editor',
})
export class WorkflowPropertiesEditor {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;

  @Prop({mutable: true}) workflow?: WorkflowDefinition;
  @Event() workflowPropsUpdated: EventEmitter<WorkflowPropsUpdatedArgs>;
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

  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex;

  private onPropertyEditorChanged = (apply: (w: WorkflowDefinition) => void) => {
    const workflow = this.workflow;
    apply(workflow);
    return this.workflowPropsUpdated.emit({workflow});
  }

  private renderPropertiesTab = () => {
    const workflow = this.workflow;

    const workflowDetails = {
      'Definition ID': workflow.definitionId,
      'Version ID': workflow.id,
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
        <elsa-label-picker />
      </FormEntry>
      <InfoList title="Information" dictionary={workflowDetails}/>
    </div>
  };

  private renderVariablesTab = () => {
    return <div>
      TODO: Variables editor
    </div>
  };
}

WorkflowEditorTunnel.injectProps(WorkflowPropertiesEditor, ['activityDescriptors']);
