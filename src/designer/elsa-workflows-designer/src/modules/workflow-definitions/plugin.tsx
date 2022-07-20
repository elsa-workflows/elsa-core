import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityDescriptor, Plugin} from "../../models";
import newButtonItemStore from "../../data/new-button-item-store";
import {MenuItem} from "../../components/shared/context-menu/models";
import {Flowchart} from "../../components/activities/flowchart/models";
import {generateUniqueActivityName} from '../../utils/generate-activity-name';
import descriptorsStore from "../../data/descriptors-store";
import studioComponentStore from "../../data/studio-component-store";
import toolbarButtonMenuItemStore from "../../data/toolbar-button-menu-item-store";
import {ToolbarMenuItem} from "../../components/toolbar/workflow-toolbar-menu/models";
import {EventBus} from "../../services";
import toolbarComponentStore from "../../data/toolbar-component-store";
import {NotificationEventTypes} from "../notifications/event-types";
import {WorkflowDefinitionManager} from "./services/manager";
import {WorkflowDefinition, WorkflowDefinitionSummary} from "./models/entities";
import {WorkflowDefinitionUpdatedArgs} from "./models/ui";
import {PublishClickedArgs} from "./components/publish-button";
import {WorkflowDefinitionsApi} from "./services/api";

const FlowchartTypeName = 'Elsa.Flowchart';

@Service()
export class WorkflowDefinitionsPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private api: WorkflowDefinitionsApi;
  private workflowDefinitionEditorElement: HTMLElsaWorkflowDefinitionEditorElement;
  private workflowDefinitionBrowserElement: HTMLElsaWorkflowDefinitionBrowserElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.api = Container.get(WorkflowDefinitionsApi);
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);

    const newWorkflowDefinitionItem: MenuItem = {
      text: 'Workflow Definition',
      clickHandler: this.onNewWorkflowDefinitionClick
    }

    const workflowDefinitionBrowserItem: ToolbarMenuItem = {
      text: 'Workflow Definitions',
      onClick: this.onBrowseWorkflowDefinitions,
      order: 5
    };

    newButtonItemStore.items = [...newButtonItemStore.items, newWorkflowDefinitionItem];
    toolbarButtonMenuItemStore.items = [...toolbarButtonMenuItemStore.items, workflowDefinitionBrowserItem];

    studioComponentStore.modalComponents = [
      ...studioComponentStore.modalComponents,
      () => <elsa-workflow-definition-browser ref={el => this.workflowDefinitionBrowserElement = el} onWorkflowDefinitionSelected={this.onWorkflowDefinitionSelected}/>];
  }

  async initialize(): Promise<void> {
  }

  newWorkflow = async () => {

    const flowchartDescriptor = this.getFlowchartDescriptor();
    const newName = await this.generateUniqueActivityName(flowchartDescriptor);

    const flowchart = {
      typeName: flowchartDescriptor.activityType,
      activities: [],
      connections: [],
      id: newName,
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;

    const workflowDefinition: WorkflowDefinition = {
      root: flowchart,
      id: '',
      name: 'Workflow 1',
      definitionId: '',
      version: 1,
      isLatest: true,
      isPublished: false,
      materializerName: 'Json'
    };

    this.showWorkflowDefinitionEditor(workflowDefinition);
  };

  private getFlowchartDescriptor = () => this.getActivityDescriptor(FlowchartTypeName);
  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.activityType == typeName)
  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => await generateUniqueActivityName([], activityDescriptor);

  private saveWorkflowDefinition = async (definition: WorkflowDefinition, publish: boolean): Promise<WorkflowDefinition> => {
    const updatedWorkflow = await this.workflowDefinitionManager.saveWorkflow(definition, publish);
    await this.workflowDefinitionEditorElement.updateWorkflowDefinition(updatedWorkflow);
    return updatedWorkflow;
  }

  private showWorkflowDefinitionEditor = (workflowDefinition: WorkflowDefinition) => {
    toolbarComponentStore.components = [() => <elsa-workflow-publish-button onPublishClicked={this.onPublishClicked}/>];
    studioComponentStore.activeComponentFactory = () => <elsa-workflow-definition-editor workflowDefinition={workflowDefinition} onWorkflowUpdated={this.onWorkflowUpdated} ref={el => this.workflowDefinitionEditorElement = el}/>;
  };

  private onNewWorkflowDefinitionClick = async () => await this.newWorkflow();

  private onWorkflowUpdated = async (e: CustomEvent<WorkflowDefinitionUpdatedArgs>) => {
    const workflowDefinition = e.detail.workflowDefinition;
    await this.saveWorkflowDefinition(workflowDefinition, false);
  }

  private onBrowseWorkflowDefinitions = async () => {
    await this.workflowDefinitionBrowserElement.show();
  }

  private onWorkflowDefinitionSelected = async (e: CustomEvent<WorkflowDefinitionSummary>) => {
    const definitionId = e.detail.definitionId;
    const workflowDefinition = await this.api.get({definitionId});
    this.showWorkflowDefinitionEditor(workflowDefinition);
  }

  private onPublishClicked = async (e: CustomEvent<PublishClickedArgs>) => {
    e.detail.begin();
    const workflowDefinition = await this.workflowDefinitionEditorElement.getWorkflowDefinition();
    await this.eventBus.emit(NotificationEventTypes.Add, this, {id: workflowDefinition.definitionId, message: `Starting publishing ${workflowDefinition.name}`});
    await this.saveWorkflowDefinition(workflowDefinition, true);
    await this.eventBus.emit(NotificationEventTypes.Update, this, {id: workflowDefinition.definitionId, message: `${workflowDefinition.name} publish finished`});
    e.detail.complete();
  }
}
