import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import WorkflowEditorTunnel from '../state';
import {TabChangedArgs, TabDefinition, Workflow
} from '../../../models';
import {FormEntry} from "../../shared/forms/form-entry";
import {InfoList} from "../../shared/forms/info-list";

export interface WorkflowPropsUpdatedArgs {
  workflow: Workflow;
}

@Component({
  tag: 'elsa-workflow-properties-editor',
})
export class WorkflowPropertiesEditor {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;

  @Prop({mutable: true}) workflow?: Workflow;
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

  private onPropertyEditorChanged = (apply: (w: Workflow) => void) => {
    const workflow = this.workflow;
    apply(workflow);
    return this.workflowPropsUpdated.emit({workflow});
  }

  private renderPropertiesTab = () => {
    const workflow = this.workflow;
    const metadata = workflow.metadata;
    const identity = workflow.identity;
    const publication = workflow.publication;

    const workflowDetails = {
      'ID': identity.id,
      'Definition ID': identity.definitionId,
      'Version': identity.version,
      'Status': publication.isPublished ? 'Published' : 'Draft'
    };

    return <div>
      <FormEntry label="Name" fieldId="workflowName" hint="The name of the workflow.">
        <input type="text" name="workflowName" id="workflowName" value={metadata.name} onChange={e => this.onPropertyEditorChanged(wf => wf.metadata.name = (e.target as HTMLInputElement).value)}/>
      </FormEntry>
      <FormEntry label="Description" fieldId="workflowDescription" hint="A brief description about the workflow.">
        <textarea name="workflowDescription" id="workflowDescription" value={metadata.description} rows={6} onChange={e => this.onPropertyEditorChanged(wf => wf.metadata.description = (e.target as HTMLTextAreaElement).value)}/>
      </FormEntry>
      <InfoList title="Workflow Information" dictionary={workflowDetails}/>
    </div>
  };

  private renderVariablesTab = () => {
    return <div>
      TODO: Variables editor
    </div>
  };
}

WorkflowEditorTunnel.injectProps(WorkflowPropertiesEditor, ['activityDescriptors']);
