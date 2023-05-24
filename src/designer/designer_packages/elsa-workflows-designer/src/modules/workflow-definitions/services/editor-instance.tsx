import {WorkflowDefinition} from "../models/entities";
import studioComponentStore from "../../../data/studio-component-store";
import {h} from "@stencil/core";
import {WorkflowDefinitionUpdatedArgs} from "../models/ui";
import NotificationService from "../../notifications/notification-service";
import {uuid} from "@antv/x6/es/util/string/uuid";
import {NotificationDisplayType} from "../../notifications/models";
import {Container} from "typedi";
import {WorkflowDefinitionManager} from "./manager";
import {PublishClickedArgs} from "../components/publish-button";
import {WorkflowDefinitionsApi} from "./api";
import {ActivityDescriptorManager} from "../../../services";
import toolbarComponentStore from "../../../data/toolbar-component-store";
import {htmlToElement} from "../../../utils";
import {WorkflowDefinitionEditorService} from "./editor-service";

export class WorkflowDefinitionEditorInstance {

  private readonly api: WorkflowDefinitionsApi;
  private readonly activityDescriptorManager: ActivityDescriptorManager;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private readonly workflowDefinitionEditorService: WorkflowDefinitionEditorService;
  private workflowDefinitionEditorElement: HTMLElsaWorkflowDefinitionEditorElement

  constructor(private readonly workflowDefinition: WorkflowDefinition) {
    this.api = Container.get(WorkflowDefinitionsApi);
    this.activityDescriptorManager = Container.get(ActivityDescriptorManager);
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
    this.workflowDefinitionEditorService = Container.get(WorkflowDefinitionEditorService);

      toolbarComponentStore.components = [() => <elsa-workflow-publish-button
        onPublishClicked={this.onPublishClicked}
        onUnPublishClicked={this.onUnPublishClicked}
        onExportClicked={this.onExportClicked}
        onImportClicked={this.onImportClicked}
        disabled={workflowDefinition.isReadonly}
      />];

    studioComponentStore.activeComponentFactory = () => <elsa-workflow-definition-editor
      workflowDefinition={workflowDefinition} onWorkflowUpdated={this.onWorkflowUpdated}
      ref={el => this.workflowDefinitionEditorElement = el}/>;
  }

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
      this.workflowDefinitionEditorService.show(importedWorkflow);
    });
  };

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

        if(definition.options?.autoUpdateConsumingWorkflows)
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

  private onWorkflowUpdated = async (e: CustomEvent<WorkflowDefinitionUpdatedArgs>) => {
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

  saveWorkflowDefinition = async (definition: WorkflowDefinition, publish: boolean): Promise<WorkflowDefinition> => {

    if (!definition.isLatest) {
      console.debug('Workflow definition is not latest. Skipping save.');
      return;
    }

    const workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
    const updatedWorkflow = await workflowDefinitionManager.saveWorkflow(definition, publish);
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

  private updateCompositeActivityReferences = async (definition: WorkflowDefinition) => {

    await this.api.updateWorkflowReferences({definitionId: definition.definitionId})
      .then(async (response) => {
        var message = 'The following consuming workflows have been successfully updated:\n\n' + response.affectedWorkflows.join('\n');
        if (response.affectedWorkflows.length > 0){
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
}
