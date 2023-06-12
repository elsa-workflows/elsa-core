import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityDescriptor, Plugin, WorkflowInstance, WorkflowInstanceSummary} from "../../../models";
import newButtonItemStore from "../../../data/new-button-item-store";
import {Flowchart} from "../../flowchart/models";
import {generateUniqueActivityName} from '../../../utils/generate-activity-name';
import descriptorsStore from "../../../data/descriptors-store";
import toolbarButtonMenuItemStore from "../../../data/toolbar-button-menu-item-store";
import {ToolbarMenuItem} from "../../../components/toolbar/workflow-toolbar-menu/models";
import {ActivityDescriptorManager, EventBus, InputControlRegistry} from "../../../services";
import {WorkflowDefinitionManager} from "../services/manager";
import {WorkflowDefinition, WorkflowDefinitionSummary} from "../models/entities";
import {PublishClickedArgs} from "../components/publish-button";
import {WorkflowDefinitionsApi} from "../services/api";
import {DefaultModalActions, ModalDialogInstance, ModalDialogService} from "../../../components/shared/modal-dialog";
import {htmlToElement} from "../../../utils";
import NotificationService from "../../notifications/notification-service";
import {uuid} from "@antv/x6/es/util/string/uuid";
import {DropdownButtonItem} from "../../../components/shared/dropdown-button/models";
import {NotificationDisplayType} from "../../notifications/models";
import {WorkflowDefinitionEditorInstance} from "../services/editor-instance";
import {WorkflowDefinitionEditorService} from "../services/editor-service";
import { WorkflowInstanceViewerService } from '../../workflow-instances/services/viewer-service';
import { WorkflowInstancesApi } from '../../workflow-instances/services/workflow-instances-api';

const FlowchartTypeName = 'Elsa.Flowchart';

@Service()
export class WorkflowDefinitionsPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private readonly workflowDefinitionEditorService: WorkflowDefinitionEditorService;
  private readonly modalDialogService: ModalDialogService;
  private readonly activityDescriptorManager: ActivityDescriptorManager;
  private api: WorkflowDefinitionsApi;
  private workflowDefinitionEditorElement: HTMLElsaWorkflowDefinitionEditorElement;
  private workflowDefinitionBrowserInstance: ModalDialogInstance;
  private inputControlRegistry: InputControlRegistry;
  private workflowDefinitionEditorInstance: WorkflowDefinitionEditorInstance;
  private readonly workflowInstancesApi: WorkflowInstancesApi;
  private workflowInstanceBrowserInstance: ModalDialogInstance;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.api = Container.get(WorkflowDefinitionsApi);
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
    this.workflowDefinitionEditorService = Container.get(WorkflowDefinitionEditorService);
    this.modalDialogService = Container.get(ModalDialogService);
    this.activityDescriptorManager = Container.get(ActivityDescriptorManager);
    this.inputControlRegistry = Container.get(InputControlRegistry)
    this.workflowInstancesApi = Container.get(WorkflowInstancesApi);

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
      isReadonly:false,
      materializerName: 'Json'
    };

    const newWorkflowDefinition = await this.workflowDefinitionManager.saveWorkflow(workflowDefinition, false);
    this.showWorkflowDefinitionEditor(newWorkflowDefinition);
  };

  private getFlowchartDescriptor = () => this.getActivityDescriptor(FlowchartTypeName);
  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.typeName == typeName)
  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => await generateUniqueActivityName([], activityDescriptor);

  public showWorkflowDefinitionEditor = (workflowDefinition: WorkflowDefinition) => {
    this.workflowDefinitionEditorInstance = this.workflowDefinitionEditorService.show(workflowDefinition);
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

  private onBrowseWorkflowDefinitions = async () => {
    const closeAction = DefaultModalActions.Close();
    const newAction = DefaultModalActions.New(this.onNewWorkflowDefinitionSelected);
    const actions = [closeAction, newAction];

    this.workflowDefinitionBrowserInstance = this.modalDialogService.show(() =>
        <elsa-workflow-definition-browser onWorkflowDefinitionSelected={this.onWorkflowDefinitionSelected}
                                          onWorkflowInstancesSelected={this.onWorkflowInstancesSelected}
                                          onNewWorkflowDefinitionSelected={this.onNewWorkflowDefinitionSelected}/>,
      {actions})
  }

  private onWorkflowDefinitionSelected = async (e: CustomEvent<WorkflowDefinitionSummary>) => {
    const definitionId = e.detail.definitionId;
    const workflowDefinition = await this.api.get({definitionId});
    this.showWorkflowDefinitionEditor(workflowDefinition);
    this.modalDialogService.hide(this.workflowDefinitionBrowserInstance);
  }

  private onWorkflowInstancesSelected = async (e: CustomEvent<WorkflowDefinitionSummary>) => {
    const definitionId = e.detail.definitionId;
    const version = e.detail.version;
    const workflow = await this.api.get({definitionId, versionOptions:{version}});

    this.workflowInstanceBrowserInstance = this.modalDialogService.show(() =>
        <elsa-workflow-instance-browser 
          workflowDefinition={workflow} 
          onWorkflowInstanceSelected={this.onWorkflowInstanceSelected}/>,
          {actions: [DefaultModalActions.Close()], size: 'tw-max-w-screen-2xl'});
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

    await this.workflowDefinitionEditorInstance.saveWorkflowDefinition(definition, true)
      .then(async () => {
        NotificationService.updateNotification(notification, {title: 'Workflow published', text: 'Published!'})
        e.detail.complete();

        if (definition.options?.autoUpdateConsumingWorkflows)
          await this.updateCompositeActivityReferences(definition);

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

  private updateCompositeActivityReferences = async (definition: WorkflowDefinition) => {

    await this.api.updateWorkflowReferences({definitionId: definition.definitionId})
      .then(async (response) => {
        const message = 'The following consuming workflows have been successfully updated:\n\n' + response.affectedWorkflows.join('\n');
        if (response.affectedWorkflows.length > 0) {
          NotificationService.createNotification({
            title: 'Consuming Workflows',
            id: uuid(),
            text: message,
            type: NotificationDisplayType.Success
          });
        }
      }).catch(() => {
        NotificationService.createNotification({
          title: 'Error while updating consuming workflows',
          id: uuid(),
          text: 'Consuming workflows could not be updated',
          type: NotificationDisplayType.Error
        });
      });
  }

  private onWorkflowInstanceSelected = async (e: CustomEvent<WorkflowInstanceSummary>) => {
    const definitionId = e.detail.definitionId;
    const instanceId = e.detail.id;
    const version = e.detail.version;

    await this.api.get({definitionId, versionOptions: {version}, includeCompositeRoot: true})
      .then(async (workflowDefinition) => {
        await this.workflowInstancesApi.get({id: instanceId}).then((workflowInstance) => {
          this.showWorkflowInstanceViewer(workflowDefinition, workflowInstance);
          this.modalDialogService.hide(this.workflowInstanceBrowserInstance);
          this.modalDialogService.hide(this.workflowDefinitionBrowserInstance);
        }).catch(() => {
          NotificationService.createNotification({
            title: 'Error',
            id: uuid(),
            text: <div>Could not load workflow instance {instanceId} information</div>,
            type: NotificationDisplayType.Error
          });
          this.modalDialogService.hide(this.workflowInstanceBrowserInstance);
        });
      }).catch(() => {
        NotificationService.createNotification({
          title: 'Error',
          id: uuid(),
          text: <div>Could not load workflow {definitionId} information</div>,
          type: NotificationDisplayType.Error
        });
        this.modalDialogService.hide(this.workflowInstanceBrowserInstance);
      });
  }

  private showWorkflowInstanceViewer = (workflowDefinition: WorkflowDefinition, workflowInstance: WorkflowInstance) => {
    const service = Container.get(WorkflowInstanceViewerService);
    service.show(workflowDefinition, workflowInstance);
  };
}
