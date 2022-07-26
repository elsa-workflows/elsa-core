import {Component, Element, h, Host, Prop, Watch} from '@stencil/core';
import 'reflect-metadata';
import {Container} from 'typedi';
import {ElsaApiClientProvider, ElsaClient, EventBus, PluginRegistry, ServerSettings} from '../../services';
import {ActivityDescriptor} from '../../models';
import {MonacoEditorSettings} from "../../services/monaco-editor-settings";
import descriptorsStore from '../../data/descriptors-store';
import studioComponentStore from "../../data/studio-component-store";
import {WorkflowDefinitionManager} from "../../modules/workflow-definitions/services/manager";

@Component({
  tag: 'elsa-studio'
})
export class Studio {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private readonly pluginRegistry: PluginRegistry;
  private activityDescriptors: Array<ActivityDescriptor>;
  private elsaClient: ElsaClient;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
    this.pluginRegistry = Container.get(PluginRegistry);
  }

  @Element() private el: HTMLElsaStudioElement;
  @Prop({attribute: 'server'}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path'}) monacoLibPath: string;

  @Watch('serverUrl')
  private handleServerUrl(value: string) {
    const settings = Container.get(ServerSettings);
    settings.baseAddress = value;
  }

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;
  }

  // @Listen('workflowInstanceSelected')
  // private async handleWorkflowInstanceSelected(e: CustomEvent<WorkflowInstanceSummary>) {
  //   const workflowManagerElement = this.workflowManagerElement;
  //
  //   if (!workflowManagerElement)
  //     return;
  //
  //   const workflowInstanceSummary = e.detail;
  //   const definitionId = workflowInstanceSummary.definitionId;
  //   const version = workflowInstanceSummary.version;
  //   const versionOptions: VersionOptions = {version};
  //   const workflowDefinition = await this.elsaClient.workflowDefinitions.get({definitionId, versionOptions});
  //   const workflowInstance = await this.elsaClient.workflowInstances.get({id: workflowInstanceSummary.id});
  //
  //   workflowManagerElement.workflowDefinition = workflowDefinition;
  //   workflowManagerElement.workflowInstance = workflowInstance;
  // }

  // @Listen('unPublishClicked')
  // private async handleUnPublishClicked(e: CustomEvent) {
  //   const workflowManagerElement = this.workflowManagerElement;
  //
  //   if (!workflowManagerElement) return;
  //
  //   const workflow = await workflowManagerElement.getWorkflowDefinition();
  //   await this.eventBus.emit(NotificationEventTypes.Add, this, {id: workflow.definitionId, message: `Starting unpublishing ${workflow.name}`});
  //   await this.retractWorkflowDefinition(workflow);
  //   await this.eventBus.emit(NotificationEventTypes.Update, this, {id: workflow.definitionId, message: `${workflow.name} unpublish finished`});
  // }
  //
  // @Listen('exportClicked')
  // private async handleExportClick(e: CustomEvent) {
  //   const workflowManagerElement = this.workflowManagerElement;
  //
  //   if (!workflowManagerElement)
  //     return;
  //
  //   const workflow = await workflowManagerElement.getWorkflowDefinition();
  //
  //   const request: ExportWorkflowRequest = {
  //     definitionId: workflow.definitionId,
  //     versionOptions: {version: workflow.version}
  //   };
  //
  //   const response = await this.elsaClient.workflowDefinitions.export(request);
  //   downloadFromBlob(response.data, {contentType: 'application/json', fileName: response.fileName});
  // }
  //
  // @Listen('importClicked')
  // private async handleImportClick(e: CustomEvent<File>) {
  //   const workflowManagerElement = this.workflowManagerElement;
  //
  //   if (!workflowManagerElement)
  //     return;
  //
  //   const file = e.detail;
  //   const client = this.elsaClient;
  //   const workflow = await workflowManagerElement.getWorkflowDefinition();
  //   const definitionId = workflow?.definitionId;
  //
  //   const importWorkflow = async (): Promise<WorkflowDefinition> => {
  //     try {
  //       const importRequest: ImportWorkflowRequest = {definitionId, file};
  //       const importResponse = await client.workflowDefinitions.import(importRequest);
  //       return importResponse.workflowDefinition;
  //     } catch (e) {
  //       console.error(e);
  //     }
  //   };
  //
  //   const workflowDefinition = await importWorkflow();
  //
  //   if (!!workflowDefinition)
  //     workflowManagerElement.workflowDefinition = workflowDefinition;
  // }

  async componentWillLoad() {
    this.handleMonacoLibPath(this.monacoLibPath);
    this.handleServerUrl(this.serverUrl);

    const elsaClientProvider = Container.get(ElsaApiClientProvider);

    this.elsaClient = await elsaClientProvider.getElsaClient();
    this.activityDescriptors = await this.elsaClient.descriptors.activities.list();
    const storageDrivers = await this.elsaClient.descriptors.storageDrivers.list();

    descriptorsStore.activityDescriptors = this.activityDescriptors;
    descriptorsStore.storageDrivers = storageDrivers;

    await this.pluginRegistry.initialize();
  }

  // private retractWorkflowDefinition = async (definition: WorkflowDefinition): Promise<WorkflowDefinition> => {
  //   const updatedWorkflow = await this.workflowDefinitionManager.retractWorkflow(definition);
  //   await this.workflowManagerElement.updateWorkflowDefinition(updatedWorkflow);
  //   return updatedWorkflow;
  // }

  render() {
    return <Host>
      {studioComponentStore.activeComponentFactory()}
      {studioComponentStore.modalComponents.map(modal => modal())}
      <elsa-modal-dialog-container/>
    </Host>;
  }
}
