import 'reflect-metadata';
import {Container, Service} from "typedi";
import {Plugin, WorkflowInstance, WorkflowInstanceSummary} from "../../models";
import {EventBus} from "../../services";
import {WorkflowDefinitionsApi} from "../workflow-definitions/services/api";
import {DefaultModalActions, ModalDialogInstance, ModalDialogService} from "../../components/shared/modal-dialog";
import {ToolbarMenuItem} from "../../components/toolbar/workflow-toolbar-menu/models";
import toolbarButtonMenuItemStore from "../../data/toolbar-button-menu-item-store";
import {WorkflowInstancesApi} from "./services/workflow-instances-api";
import {WorkflowDefinition} from "../workflow-definitions/models/entities";
import {h} from "@stencil/core";
import studioComponentStore from "../../data/studio-component-store";
import NotificationService from "../notifications/notification-service";
import {uuid} from "@antv/x6/es/util/string/uuid";
import {NotificationDisplayType} from "../notifications/models";

@Service()
export class WorkflowInstancesPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionsApi: WorkflowDefinitionsApi;
  private readonly workflowInstancesApi: WorkflowInstancesApi;
  private readonly modalDialogService: ModalDialogService;
  private workflowInstanceBrowserInstance: ModalDialogInstance;
  private workflowInstanceViewerElement: HTMLElsaWorkflowInstanceViewerElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.workflowDefinitionsApi = Container.get(WorkflowDefinitionsApi);
    this.workflowInstancesApi = Container.get(WorkflowInstancesApi);
    this.modalDialogService = Container.get(ModalDialogService);

    const workflowInstanceBrowserItem: ToolbarMenuItem = {
      text: 'Workflow Instances',
      onClick: this.onBrowseWorkflowInstances,
      order: 5
    };

    toolbarButtonMenuItemStore.items = [...toolbarButtonMenuItemStore.items, workflowInstanceBrowserItem];
  }

  async initialize(): Promise<void> {

  }

  private showWorkflowInstanceViewer = (workflowDefinition: WorkflowDefinition, workflowInstance: WorkflowInstance) => {
    studioComponentStore.activeComponentFactory = () =>
      <elsa-workflow-instance-viewer
        workflowDefinition={workflowDefinition}
        workflowInstance={workflowInstance}
        ref={el => this.workflowInstanceViewerElement = el}/>;
  };

  private onBrowseWorkflowInstances = async () => {
    const closeAction = DefaultModalActions.Close();
    const actions = [closeAction];

    this.workflowInstanceBrowserInstance = this.modalDialogService.show(() =>
        <elsa-workflow-instance-browser onWorkflowInstanceSelected={this.onWorkflowInstanceSelected}/>,
      {actions: actions, size: 'max-w-screen-2xl'})
  }

  private onWorkflowInstanceSelected = async (e: CustomEvent<WorkflowInstanceSummary>) => {
    const definitionId = e.detail.definitionId;
    const instanceId = e.detail.id;
    const version = e.detail.version;

    await this.workflowDefinitionsApi.get({definitionId, versionOptions: {version}, includeCompositeRoot: true})
      .then(async (workflowDefinition) => {
        await this.workflowInstancesApi.get({id: instanceId}).then((workflowInstance) => {
          this.showWorkflowInstanceViewer(workflowDefinition, workflowInstance);
          this.modalDialogService.hide(this.workflowInstanceBrowserInstance);
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
}
