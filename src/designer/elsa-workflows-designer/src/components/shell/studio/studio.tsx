import {Component, Element, h, Listen, Prop, Watch} from '@stencil/core';
import 'reflect-metadata';
import {Container} from 'typedi';
import {v4 as uuid} from 'uuid';
import {
  ElsaApiClientProvider,
  ElsaClient,
  ServerSettings
} from '../../../services';
import {ActivityDescriptor, VersionOptions, WorkflowDefinition, WorkflowInstanceSummary, WorkflowDefinitionSummary} from '../../../models';
import {PublishClickedArgs} from "../../toolbar/workflow-publish-button/workflow-publish-button";
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {downloadFromBlob} from "../../../utils";
import {ExportWorkflowRequest, ImportWorkflowRequest} from "../../../services/api-client/workflow-definitions-api";
import {WorkflowDefinitionManager} from "../../../services/workflow-definition-manager";
import {WorkflowDefinitionUpdatedArgs} from "../../designer/workflow-definition-editor/models";
import {Flowchart} from "../../activities/flowchart/models";

@Component({
  tag: 'elsa-studio'
})
export class Studio {
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private activityDescriptors: Array<ActivityDescriptor>;
  private elsaClient: ElsaClient;
  private workflowManagerElement?: HTMLElsaWorkflowManagerElement;

  constructor() {
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
  }

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

    if (!!this.workflowManagerElement)
      this.workflowManagerElement.monacoLibPath = this.monacoLibPath;
  }

  @Listen('workflowUpdated')
  private async handleWorkflowUpdated(e: CustomEvent<WorkflowDefinitionUpdatedArgs>) {
    const workflowDefinition = e.detail.workflowDefinition;
    await this.saveWorkflowDefinition(workflowDefinition, false);
  }

  @Listen('workflowDefinitionSelected')
  private async handleWorkflowDefinitionSelected(e: CustomEvent<WorkflowDefinitionSummary>) {
    const workflowManagerElement = this.workflowManagerElement;

    if (!workflowManagerElement)
      return;

    const definitionId = e.detail.definitionId;
    const workflowDefinition = await this.elsaClient.workflowDefinitions.get({definitionId});
    workflowManagerElement.workflowInstance = null;
    workflowManagerElement.workflowDefinition = workflowDefinition;
  }

  @Listen('workflowInstanceSelected')
  private async handleWorkflowInstanceSelected(e: CustomEvent<WorkflowInstanceSummary>) {
    const workflowManagerElement = this.workflowManagerElement;

    if (!workflowManagerElement)
      return;

    const workflowInstanceSummary = e.detail;
    const definitionId = workflowInstanceSummary.definitionId;
    const version = workflowInstanceSummary.version;
    const versionOptions: VersionOptions = {version};
    const workflowDefinition = await this.elsaClient.workflowDefinitions.get({definitionId, versionOptions});
    const workflowInstance = await this.elsaClient.workflowInstances.get({id: workflowInstanceSummary.id});

    workflowManagerElement.workflowDefinition = workflowDefinition;
    workflowManagerElement.workflowInstance = workflowInstance;
  }

  @Listen('publishClicked')
  private async handlePublishClicked(e: CustomEvent<PublishClickedArgs>) {
    const workflowManagerElement = this.workflowManagerElement;

    if (!workflowManagerElement)
      return;

    e.detail.begin();
    const workflowDefinition = await workflowManagerElement.getWorkflowDefinition();
    await this.saveWorkflowDefinition(workflowDefinition, true);
    e.detail.complete();
  }

  @Listen('unPublishClicked')
  private async handleUnPublishClicked(e: CustomEvent) {
    const workflowManagerElement = this.workflowManagerElement;

    if (!workflowManagerElement)
      return;

    const workflow = await workflowManagerElement.getWorkflowDefinition();
    await this.retractWorkflowDefinition(workflow);
  }

  @Listen('newClicked')
  private async handleNewClick(e: CustomEvent) {
    const workflowManagerElement = this.workflowManagerElement;

    if (!workflowManagerElement)
      return;

    const flowchart = {
      typeName: 'Elsa.Flowchart',
      activities: [],
      connections: [],
      id: uuid(),
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;

    const workflowDefinition: WorkflowDefinition = {
      root: flowchart,
      id: '',
      definitionId: '',
      version: 1,
      isLatest: true,
      isPublished: false,
      materializerName: 'Json'
    }

    this.workflowManagerElement.workflowDefinition = workflowDefinition;
  }

  @Listen('exportClicked')
  private async handleExportClick(e: CustomEvent) {
    const workflowManagerElement = this.workflowManagerElement;

    if (!workflowManagerElement)
      return;

    const workflow = await workflowManagerElement.getWorkflowDefinition();

    const request: ExportWorkflowRequest = {
      definitionId: workflow.definitionId,
      versionOptions: {version: workflow.version}
    };

    const response = await this.elsaClient.workflowDefinitions.export(request);
    downloadFromBlob(response.data, {contentType: 'application/json', fileName: response.fileName});
  }

  @Listen('importClicked')
  private async handleImportClick(e: CustomEvent<File>) {
    const workflowManagerElement = this.workflowManagerElement;

    if (!workflowManagerElement)
      return;

    const file = e.detail;
    const client = this.elsaClient;
    const workflow = await workflowManagerElement.getWorkflowDefinition();
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

    if (!!workflowDefinition)
      workflowManagerElement.workflowDefinition = workflowDefinition;
  }

  public async componentWillLoad() {
    this.handleServerUrl(this.serverUrl);

    const elsaClientProvider = Container.get(ElsaApiClientProvider);

    this.elsaClient = await elsaClientProvider.getElsaClient();
    this.activityDescriptors = await this.elsaClient.descriptors.activities.list();

    this.workflowManagerElement = this.el.getElementsByTagName('elsa-workflow-manager')[0] as HTMLElsaWorkflowManagerElement;

    if (!!this.workflowManagerElement) {
      this.workflowManagerElement.activityDescriptors = this.activityDescriptors;
      this.workflowManagerElement.monacoLibPath = this.monacoLibPath;
    }
  }

  public render() {
    return <slot/>;
  }

  private saveWorkflowDefinition = async (definition: WorkflowDefinition, publish: boolean): Promise<WorkflowDefinition> => {
    const updatedWorkflow = await this.workflowDefinitionManager.saveWorkflow(definition, publish);
    await this.workflowManagerElement.updateWorkflowDefinition(updatedWorkflow);
    return updatedWorkflow;
  }

  private retractWorkflowDefinition = async (definition: WorkflowDefinition): Promise<WorkflowDefinition> => {
    const updatedWorkflow = await this.workflowDefinitionManager.retractWorkflow(definition);
    await this.workflowManagerElement.updateWorkflowDefinition(updatedWorkflow);
    return updatedWorkflow;
  }
}
