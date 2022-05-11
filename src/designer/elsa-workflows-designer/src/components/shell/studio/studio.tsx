import {Component, Element, EventEmitter, h, Listen, Prop, Watch} from '@stencil/core';
import 'reflect-metadata';
import {Container} from 'typedi';
import {
  ElsaApiClientProvider,
  ElsaClient, ExportWorkflowRequest, ImportWorkflowRequest,
  RetractWorkflowDefinitionRequest,
  SaveWorkflowDefinitionRequest,
  ServerSettings
} from '../../../services';
import {ActivityDescriptor, VersionOptions, WorkflowDefinition, WorkflowInstanceSummary, WorkflowDefinitionSummary} from '../../../models';
import {WorkflowUpdatedArgs} from '../../designer/workflow-editor/workflow-editor';
import {PublishClickedArgs} from "../../toolbar/workflow-publish-button/workflow-publish-button";
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {downloadFromBlob, getVersionOptionsString} from "../../../utils";

@Component({
  tag: 'elsa-studio'
})
export class Studio {
  private activityDescriptors: Array<ActivityDescriptor>;
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
  private async handleWorkflowDefinitionSelected(e: CustomEvent<WorkflowDefinitionSummary>) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    const definitionId = e.detail.definitionId;
    const workflowDefinition = await this.elsaClient.workflowDefinitions.get({definitionId});
    await workflowEditorElement.importWorkflow(workflowDefinition);
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
    const workflow = await this.elsaClient.workflowDefinitions.get({definitionId, versionOptions});
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

  @Listen('unPublishClicked')
  private async handleUnPublishClicked(e: CustomEvent) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    const workflow = await workflowEditorElement.getWorkflow();
    await this.retractWorkflow(workflow);
  }

  @Listen('newClicked')
  private async handleNewClick(e: CustomEvent) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    const request: SaveWorkflowDefinitionRequest = {
      definitionId: "",
      publish: false
    };

    const updatedWorkflow = await this.elsaClient.workflowDefinitions.post(request);
    await this.workflowEditorElement.importWorkflowMetadata(updatedWorkflow);
  }

  @Listen('exportClicked')
  private async handleExportClick(e: CustomEvent) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    const workflow = await workflowEditorElement.getWorkflow();

    const request: ExportWorkflowRequest = {
      definitionId: workflow.definitionId,
      versionOptions: {version: workflow.version}
    };

    const response = await this.elsaClient.workflowDefinitions.export(request);
    downloadFromBlob(response.data, {contentType: 'application/json', fileName: response.fileName});
  }

  @Listen('importClicked')
  private async handleImportClick(e: CustomEvent<File>) {
    const workflowEditorElement = this.workflowEditorElement;

    if (!workflowEditorElement)
      return;

    const file = e.detail;
    const client = this.elsaClient;
    const workflow = await workflowEditorElement.getWorkflow();
    const definitionId = workflow?.definitionId;

    const importWorkflow = async (): Promise<WorkflowDefinition> => {
      try {
        const importRequest: ImportWorkflowRequest = {definitionId, file};
        const importResponse = await client.workflowDefinitions.import(importRequest);
        return importResponse.workflowDefinition;
      } catch (e) {
        console.error(e);
      }
    };

    const workflowDefinition = await importWorkflow();

    if(!!workflowDefinition)
      await workflowEditorElement.importWorkflow(workflowDefinition);
  }

  public async componentWillLoad() {
    this.handleServerUrl(this.serverUrl);

    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getClient();
    this.activityDescriptors = await this.elsaClient.descriptors.activities.list();
    this.workflowEditorElement = this.el.getElementsByTagName('elsa-workflow-editor')[0] as HTMLElsaWorkflowEditorElement;

    if (!!this.workflowEditorElement) {
      this.workflowEditorElement.activityDescriptors = this.activityDescriptors;
      this.workflowEditorElement.monacoLibPath = this.monacoLibPath;
    }
  }

  public render() {
    return <slot/>;
  }

  private saveWorkflow = async (definition: WorkflowDefinition, publish: boolean): Promise<WorkflowDefinition> => {
    const request: SaveWorkflowDefinitionRequest = {
      definitionId: definition.definitionId,
      name: definition.name,
      description: definition.description,
      publish: publish,
      root: definition.root
    };

    const updatedWorkflow = await this.elsaClient.workflowDefinitions.post(request);
    await this.workflowEditorElement.importWorkflowMetadata(updatedWorkflow);
    return updatedWorkflow;
  }

  private retractWorkflow = async (workflow: WorkflowDefinition): Promise<WorkflowDefinition> => {
    const request: RetractWorkflowDefinitionRequest = {
      definitionId: workflow.definitionId
    };

    const updatedWorkflow = await this.elsaClient.workflowDefinitions.retract(request);
    await this.workflowEditorElement.importWorkflowMetadata(updatedWorkflow);
    return updatedWorkflow;
  }
}
