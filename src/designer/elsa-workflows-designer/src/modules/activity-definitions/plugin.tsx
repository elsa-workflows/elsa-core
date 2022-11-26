import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityDescriptor, Plugin} from "../../models";
import newButtonItemStore from "../../data/new-button-item-store";
import {MenuItem, MenuItemGroup} from "../../components/shared/context-menu/models";
import {Flowchart} from "../flowchart/models";
import {generateUniqueActivityName} from '../../utils/generate-activity-name';
import descriptorsStore from "../../data/descriptors-store";
import studioComponentStore from "../../data/studio-component-store";
import toolbarButtonMenuItemStore from "../../data/toolbar-button-menu-item-store";
import {ToolbarMenuItem} from "../../components/toolbar/workflow-toolbar-menu/models";
import {ActivityDescriptorManager, EventBus} from "../../services";
import toolbarComponentStore from "../../data/toolbar-component-store";
import {NotificationEventTypes} from "../notifications/event-types";
import {PublishClickedArgs} from "./components/publish-button";
import {ActivityDefinitionManager} from "./services/manager";
import {ActivityDefinition, ActivityDefinitionSummary, ActivityDefinitionUpdatedArgs} from "./models";
import {ActivityDefinitionsApi} from "./services/api";
import {DefaultModalActions, ModalDialogInstance, ModalDialogService} from "../../components/shared/modal-dialog";

const FlowchartTypeName = 'Elsa.Flowchart';

@Service()
export class ActivityDefinitionsPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly activityDefinitionManager: ActivityDefinitionManager;
  private readonly activityDescriptorManager: ActivityDescriptorManager;
  private readonly api: ActivityDefinitionsApi;
  private readonly modalDialogService: ModalDialogService;
  private activityDefinitionEditorElement: HTMLElsaActivityDefinitionEditorElement;
  private activityDefinitionBrowserInstance: ModalDialogInstance;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.activityDefinitionManager = Container.get(ActivityDefinitionManager);
    this.api = Container.get(ActivityDefinitionsApi);
    this.activityDescriptorManager = Container.get(ActivityDescriptorManager);
    this.modalDialogService = Container.get(ModalDialogService);

    const newActivityDefinitionItem: MenuItem = {
      text: 'Activity Definition',
      clickHandler: this.onNewActivityDefinitionClick
    }

    const newItemGroup: MenuItemGroup = {
      menuItems: [newActivityDefinitionItem]
    };

    const activityDefinitionBrowserItem: ToolbarMenuItem = {
      text: 'Activity Definitions',
      onClick: this.onBrowseActivityDefinitions,
      order: 5
    };

    newButtonItemStore.items = [...newButtonItemStore.items, newItemGroup];
    toolbarButtonMenuItemStore.items = [...toolbarButtonMenuItemStore.items, activityDefinitionBrowserItem];
  }

  async initialize(): Promise<void> {
  }

  newActivityDefinition = async () => {

    const flowchartDescriptor = this.getFlowchartDescriptor();
    const newName = await this.generateUniqueActivityName(flowchartDescriptor);

    const flowchart = {
      type: flowchartDescriptor.typeName,
      version: 1,
      activities: [],
      connections: [],
      id: newName,
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;

    const activityDefinition: ActivityDefinition = {
      root: flowchart,
      id: '',
      definitionId: '',
      type: 'Activity1',
      category: 'Custom',
      displayName: 'Activity 1',
      version: 1,
      isLatest: true,
      isPublished: false
    };

    this.showActivityDefinitionEditor(activityDefinition);
  };

  private getFlowchartDescriptor = () => this.getActivityDescriptor(FlowchartTypeName);
  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.typeName == typeName)
  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => await generateUniqueActivityName([], activityDescriptor);

  private saveActivityDefinition = async (definition: ActivityDefinition, publish: boolean): Promise<ActivityDefinition> => {
    const updatedDefinition = await this.activityDefinitionManager.save(definition, publish);
    await this.activityDefinitionEditorElement.updateActivityDefinition(updatedDefinition);
    return updatedDefinition;
  }

  private showActivityDefinitionEditor = (activityDefinition: ActivityDefinition) => {
    toolbarComponentStore.components = [() => <elsa-activity-publish-button onPublishClicked={this.onPublishClicked}/>];
    studioComponentStore.activeComponentFactory = () => <elsa-activity-definition-editor activityDefinition={activityDefinition} onActivityDefinitionUpdated={this.onActivityDefinitionUpdated}
                                                                                         ref={el => this.activityDefinitionEditorElement = el}/>;
  };

  private onNewActivityDefinitionClick = async () => {
    await this.newActivityDefinition();
    this.modalDialogService.hide(this.activityDefinitionBrowserInstance);
  };

  private onActivityDefinitionUpdated = async (e: CustomEvent<ActivityDefinitionUpdatedArgs>) => {
    const activityDefinition = e.detail.activityDefinition;
    await this.saveActivityDefinition(activityDefinition, false);
  }

  private onBrowseActivityDefinitions = async () => {
    const closeAction = DefaultModalActions.Close();
    const newAction = DefaultModalActions.New(this.onNewActivityDefinitionClick);
    const actions = [closeAction, newAction];

    this.activityDefinitionBrowserInstance = this.modalDialogService.show(() =>
        <elsa-activity-definition-browser onActivityDefinitionSelected={this.onActivityDefinitionSelected} onNewActivityDefinitionSelected={this.onNewActivityDefinitionClick}/>,
      {actions})
  }

  private onActivityDefinitionSelected = async (e: CustomEvent<ActivityDefinitionSummary>) => {
    const definitionId = e.detail.definitionId;
    const workflowDefinition = await this.api.get({definitionId});
    this.showActivityDefinitionEditor(workflowDefinition);
    this.modalDialogService.hide(this.activityDefinitionBrowserInstance);
  }

  private onPublishClicked = async (e: CustomEvent<PublishClickedArgs>) => {
    e.detail.begin();
    const activityDefinition = await this.activityDefinitionEditorElement.getActivityDefinition();
    await this.eventBus.emit(NotificationEventTypes.Add, this, {id: activityDefinition.definitionId, message: `Starting publishing ${activityDefinition.type}`});
    await this.saveActivityDefinition(activityDefinition, true);
    await this.eventBus.emit(NotificationEventTypes.Update, this, {id: activityDefinition.definitionId, message: `${activityDefinition.type} publish finished`});
    e.detail.complete();

    // Reload activity descriptors.
    await this.activityDescriptorManager.refresh();
  }
}
