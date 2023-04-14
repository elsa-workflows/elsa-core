import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityDescriptor, Plugin} from "../../../models";
import newButtonItemStore from "../../../data/new-button-item-store";
import {Flowchart} from "../../flowchart/models";
import {generateUniqueActivityName} from '../../../utils/generate-activity-name';
import descriptorsStore from "../../../data/descriptors-store";
import studioComponentStore from "../../../data/studio-component-store";
import toolbarButtonMenuItemStore from "../../../data/toolbar-button-menu-item-store";
import {ToolbarMenuItem} from "../../../components/toolbar/workflow-toolbar-menu/models";
import {ActivityDescriptorManager, EventBus, InputControlRegistry} from "../../../services";
import toolbarComponentStore from "../../../data/toolbar-component-store";
import {WorkflowDefinitionManager} from "../services/manager";
import {WorkflowDefinition, WorkflowDefinitionSummary} from "../models/entities";
import {WorkflowDefinitionUpdatedArgs} from "../models/ui";
import {PublishClickedArgs} from "../components/publish-button";
import {WorkflowDefinitionsApi} from "../services/api";
import {DefaultModalActions, ModalDialogInstance, ModalDialogService} from "../../../components/shared/modal-dialog";
import {htmlToElement} from "../../../utils";
import NotificationService from "../../notifications/notification-service";
import {uuid} from "@antv/x6/es/util/string/uuid";
import {DropdownButtonItem} from "../../../components/shared/dropdown-button/models";
import {NotificationDisplayType} from "../../notifications/models";

const FlowchartTypeName = 'Elsa.Flowchart';

@Service()
export class WorkflowDefinitionsPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private readonly modalDialogService: ModalDialogService;
  private readonly activityDescriptorManager: ActivityDescriptorManager;
  private api: WorkflowDefinitionsApi;
  private workflowDefinitionEditorElement: HTMLElsaWorkflowDefinitionEditorElement;
  private workflowDefinitionBrowserInstance: ModalDialogInstance;
  private inputControlRegistry: InputControlRegistry;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.api = Container.get(WorkflowDefinitionsApi);
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
    this.modalDialogService = Container.get(ModalDialogService);
    this.activityDescriptorManager = Container.get(ActivityDescriptorManager);
    this.inputControlRegistry = Container.get(InputControlRegistry)

    const newMenuItems: Array<DropdownButtonItem> = [{
      order: 0,
      group: 0,
      text: 'Workflow Definition',
      handler: this.onNewWorkflowDefinitionSelected
    }, {
      order: 0,
      group: 2,
      text: 'Import',
      handler: this.onImportWorkflowDefinitionClick
    }];

    const toolbarItems: Array<ToolbarMenuItem> = [{
      text: 'Workflow Definitions',
      onClick: this.onBrowseWorkflowDefinitions,
      order: 5
    }]

    newButtonItemStore.items = [...newButtonItemStore.items, ...newMenuItems];
    toolbarButtonMenuItemStore.items = [...toolbarButtonMenuItemStore.items, ...toolbarItems];
  }

  async initialize(): Promise<void> {
    this.inputControlRegistry.add("workflow-definition-picker", c => <elsa-workflow-definition-picker-input
      inputContext={c}/>);
  }

  newWorkflow = async () => {

    const flowchartDescriptor = this.getFlowchartDescriptor();
    const newName = await this.generateUniqueActivityName(flowchartDescriptor);

    const flowchart: Flowchart = {
      type: flowchartDescriptor.typeName,
      version: 1,
      activities: [],
      connections: [],
      id: newName,
      metadata: {},
      customProperties: {},
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
  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.typeName == typeName)
  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => await generateUniqueActivityName([], activityDescriptor);

  private saveWorkflowDefinition = async (definition: WorkflowDefinition, publish: boolean): Promise<WorkflowDefinition> => {

    if (!definition.isLatest) {
      console.debug('Workflow definition is not latest. Skipping save.');
      return;
    }

    const updatedWorkflow = await this.workflowDefinitionManager.saveWorkflow(definition, publish);
    let reload = false;

    if (definition.id != updatedWorkflow.id) reload = true;
    if (definition.definitionId != updatedWorkflow.definitionId) reload = true;
    if (definition.version != updatedWorkflow.version) reload = true;
    if (definition.isPublished != updatedWorkflow.isPublished) reload = true;
    if (definition.isLatest != updatedWorkflow.isLatest) reload = true;

    if (reload) {
      await this.workflowDefinitionEditorElement.updateWorkflowDefinition(updatedWorkflow);
      await this.workflowDefinitionEditorElement.loadWorkflowVersions();
    }

    return definition;
  }

  public showWorkflowDefinitionEditor = (workflowDefinition: WorkflowDefinition) => {
    toolbarComponentStore.components = [() => <elsa-workflow-publish-button onPublishClicked={this.onPublishClicked}
                                                                            onUnPublishClicked={this.onUnPublishClicked}
                                                                            onExportClicked={this.onExportClicked}
                                                                            onImportClicked={this.onImportClicked}/>];
    studioComponentStore.activeComponentFactory = () => <elsa-workflow-definition-editor
      workflowDefinition={workflowDefinition} onWorkflowUpdated={this.onWorkflowUpdated}
      ref={el => this.workflowDefinitionEditorElement = el}/>;
  };

  private import = async () => {
    const fileInput = htmlToElement<HTMLInputElement>('<input type="file" />');

    document.body.append(fileInput);
    fileInput.click();

    fileInput.addEventListener('change', async e => {
      const files = fileInput.files;

      if (files.length == 0) {
        fileInput.remove();
        return;
      }

      const file = files[0];
      const importedWorkflow = await this.workflowDefinitionManager.importWorkflow(null, file);
      fileInput.remove();
      this.showWorkflowDefinitionEditor(importedWorkflow);
    });
  };

  private onNewWorkflowDefinitionSelected = async () => {
    await this.newWorkflow();
    this.modalDialogService.hide(this.workflowDefinitionBrowserInstance);
  };

  private onImportWorkflowDefinitionClick = async () => {
    await this.import();
    this.modalDialogService.hide(this.workflowDefinitionBrowserInstance);
  };

  public onWorkflowUpdated = async (e: CustomEvent<WorkflowDefinitionUpdatedArgs>) => {
    const updatedWorkflowDefinition = e.detail.workflowDefinition;
    await this.saveWorkflowDefinition(updatedWorkflowDefinition, false)
      .catch(() => {
        NotificationService.createNotification({
          title: 'Error while saving',
          id: uuid(),
          text: <span>Workflow {e.detail.workflowDefinition.definitionId} could not be saved. </span>,
          type: NotificationDisplayType.Error
        });
      });
  }

  private onBrowseWorkflowDefinitions = async () => {
    const closeAction = DefaultModalActions.Close();
    const newAction = DefaultModalActions.New(this.onNewWorkflowDefinitionSelected);
    const actions = [closeAction, newAction];

    this.workflowDefinitionBrowserInstance = this.modalDialogService.show(() =>
        <elsa-workflow-definition-browser onWorkflowDefinitionSelected={this.onWorkflowDefinitionSelected}
                                          onNewWorkflowDefinitionSelected={this.onNewWorkflowDefinitionSelected}/>,
      {actions})
  }

  private onWorkflowDefinitionSelected = async (e: CustomEvent<WorkflowDefinitionSummary>) => {
    const definitionId = e.detail.definitionId;
    const workflowDefinition = await this.api.get({definitionId});
    this.showWorkflowDefinitionEditor(workflowDefinition);
    this.modalDialogService.hide(this.workflowDefinitionBrowserInstance);
  }

  public publishCurrentWorkflow = async (args: PublishClickedArgs) => {
    return this.onPublishClicked(new CustomEvent('PublishClickedArgs', {detail: args}));
  }

  private onPublishClicked = async (e: CustomEvent<PublishClickedArgs>) => {
    const definition = await this.workflowDefinitionEditorElement.getWorkflowDefinition();

    if (!definition.isLatest) {
      console.debug('Workflow definition is not latest. Skipping publish.');
      return;
    }

    e.detail.begin();

    const notification = NotificationService.createNotification({
      title: 'Publishing',
      id: uuid(),
      text: 'Workflow is being published. Please wait.',
      type: NotificationDisplayType.InProgress
    });

    await this.saveWorkflowDefinition(definition, true)
      .then(async () => {
        NotificationService.updateNotification(notification, {title: 'Workflow published', text: 'Published!'})
        e.detail.complete();

        // Reload activity descriptors.
        await this.activityDescriptorManager.refresh();
      }).catch(() => {
        NotificationService.updateNotification(notification, {
          title: 'Error while publishing',
          text: <span>Workflow {definition.definitionId} could not be published.</span>,
          type: NotificationDisplayType.Error
        });
        e.detail.complete();
      });
  }

  private onUnPublishClicked = async (e: CustomEvent) => {
    const definition = await this.workflowDefinitionEditorElement.getWorkflowDefinition();

    const notification = NotificationService.createNotification({
      title: 'Unpublishing',
      id: uuid(),
      text: 'Unpublishing the workflow. Please wait.',
      type: NotificationDisplayType.InProgress
    });

    await this.workflowDefinitionManager.retractWorkflow(definition)
      .then(async () => {
        NotificationService.updateNotification(notification, {title: 'Workflow unpublished', text: 'Unpublished!'})
        await this.activityDescriptorManager.refresh();
      }).catch(() => {
        NotificationService.updateNotification(notification, {
          title: 'Error while unpublishing',
          text: <span>Workflow {definition.definitionId} could not be unpublished.</span>,
          type: NotificationDisplayType.Error
        });
      });
  }

  private onExportClicked = async (e: CustomEvent) => {
    const workflowDefinition = await this.workflowDefinitionEditorElement.getWorkflowDefinition();
    await this.workflowDefinitionManager.exportWorkflow(workflowDefinition);
  }

  private onImportClicked = async (e: CustomEvent) => {
    await this.import();
  }
}
