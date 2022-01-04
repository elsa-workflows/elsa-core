import {Component, Element, h, Listen, Prop, Watch} from '@stencil/core';
import 'reflect-metadata';
import {Container} from 'typedi';
import {ElsaApiClientProvider, ElsaClient, SaveWorkflowRequest, ServerSettings} from '../../../services';
import {ActivityDescriptor, TriggerDescriptor, VersionOptions, Workflow, WorkflowInstanceSummary, WorkflowSummary} from '../../../models';
import {WorkflowUpdatedArgs} from '../../designer/workflow-editor/workflow-editor';
import {PublishClickedArgs} from "../../toolbar/workflow-publish-button/workflow-publish-button";
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";

@Component({
  tag: 'elsa-studio'
})
export class Studio {
  private activityDescriptors: Array<ActivityDescriptor>;
  private triggerDescriptors: Array<TriggerDescriptor>;
  private elsaClient: ElsaClient;
  private workflowEditorElement?: HTMLElsaWorkflowEditorElement;

  @Element() private el: HTMLElsaStudioElement;
  @Prop({attribute: 'server'}) public serverUrl: string;
  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;

  @Watch('serverUrl')
  private handleServerUrl(value: string) {
    const settings = Container.get(ServerSettings);
    settings.baseAddress = value;
  }

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;

    if (!!this.workflowEditorElement)
      this.workflowEditorElement.monacoLibPath = this.monacoLibPath;
  }

  @Listen('workflowUpdated')
  private async handleWorkflowUpdated(e: CustomEvent<WorkflowUpdatedArgs>) {
    const workflow = e.detail.workflow;
    await this.saveWorkflow(workflow, false);
  }

  @Listen('workflowDefinitionSelected')
  private async handleWorkflowDefinitionSelected(e: CustomEvent<WorkflowSummary>) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    const definitionId = e.detail.definitionId;
    const workflow = await this.elsaClient.workflows.get({definitionId});
    await workflowEditorElement.importWorkflow(workflow);
  }

  @Listen('workflowInstanceSelected')
  private async handleWorkflowInstanceSelected(e: CustomEvent<WorkflowInstanceSummary>) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    const workflowInstanceSummary = e.detail;
    const definitionId = workflowInstanceSummary.definitionId;
    const version = workflowInstanceSummary.version;
    const versionOptions: VersionOptions = {version};
    const workflow = await this.elsaClient.workflows.get({definitionId, versionOptions});
    const workflowInstance = await this.elsaClient.workflowInstances.get({id: workflowInstanceSummary.id});
    await workflowEditorElement.importWorkflow(workflow, workflowInstance);
  }

  @Listen('publishClicked')
  private async handlePublishClicked(e: CustomEvent<PublishClickedArgs>) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    e.detail.begin();
    const workflow = await workflowEditorElement.getWorkflow();
    await this.saveWorkflow(workflow, true);
    e.detail.complete();
  }

  public async componentWillLoad() {
    this.handleServerUrl(this.serverUrl);

    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getClient();
    this.activityDescriptors = await this.elsaClient.descriptors.activities.list();
    this.triggerDescriptors = await this.elsaClient.descriptors.triggers.list();
    this.workflowEditorElement = this.el.getElementsByTagName('elsa-workflow-editor')[0] as HTMLElsaWorkflowEditorElement;

    if (!!this.workflowEditorElement) {
      this.workflowEditorElement.activityDescriptors = this.activityDescriptors;
      this.workflowEditorElement.triggerDescriptors = this.triggerDescriptors;
      this.workflowEditorElement.monacoLibPath = this.monacoLibPath;
    }
  }

  public render() {
    return <slot/>;
  }

  private saveWorkflow = async (workflow: Workflow, publish: boolean): Promise<Workflow> => {
    const request: SaveWorkflowRequest = {
      definitionId: workflow.identity.definitionId,
      name: workflow.metadata.name,
      description: workflow.metadata.description,
      publish: publish,
      triggers: workflow.triggers,
      root: workflow.root
    };

    const updatedWorkflow = await this.elsaClient.workflows.post(request);
    await this.workflowEditorElement.importWorkflowMetadata(updatedWorkflow);
    return updatedWorkflow;
  }
}
