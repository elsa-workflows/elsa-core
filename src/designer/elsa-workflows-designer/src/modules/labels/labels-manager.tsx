import {Component, h, Host, Method, State} from '@stencil/core';
import {debounce} from 'lodash';
import {DefaultActions, EventTypes} from "../../models";
import {Container} from "typedi";
import labelStore from "./label-store";
import {ElsaApiClientProvider, ElsaClient, EventBus} from "../../services";
import {CreateLabelEventArgs, DeleteLabelEventArgs, UpdateLabelEventArgs} from "./models";
import {LabelsApi, WorkflowDefinitionLabelsApi} from "./labels-api";
import {WorkflowDefinitionManager} from "../../services/workflow-definition-manager";
import {
  PropertiesTabModel,
  WorkflowDefinitionImportedArgs,
  WorkflowEditorEventTypes,
  WorkflowEditorReadyArgs,
  WorkflowPropertiesEditorDisplayingArgs,
  WorkflowPropertiesEditorEventTypes
} from "../workflow-definitions/models";
import {ToolbarDisplayingArgs, ToolbarEventTypes} from "../../components/toolbar/workflow-toolbar-menu/models";
import {isNullOrWhitespace} from "../../utils";
import {FormEntry} from "../../components/shared/forms/form-entry";

@Component({
  tag: 'elsa-labels-manager',
  shadow: false,
})
export class LabelsManager {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private readonly labelsApi: LabelsApi;
  private readonly workflowDefinitionLabelsApi: WorkflowDefinitionLabelsApi;
  private readonly saveLabelsDebounced: () => void;
  private workflowEditor: HTMLElsaWorkflowDefinitionEditorElement;
  private modalDialog: HTMLElsaModalDialogElement;
  private elsaClient: ElsaClient;
  private definitionVersionId: string;
  private assignedLabelIds: Array<string> = [];

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
    this.labelsApi = Container.get(LabelsApi);
    this.workflowDefinitionLabelsApi = Container.get(WorkflowDefinitionLabelsApi);
    this.eventBus.on(WorkflowEditorEventTypes.WorkflowEditor.Ready, this.onWorkflowEditorReady)
    this.eventBus.on(ToolbarEventTypes.Displaying, this.onToolbarDisplaying);
    this.eventBus.on(WorkflowPropertiesEditorEventTypes.Displaying, this.onWorkflowPropertiesEditorDisplaying);
    this.eventBus.on(WorkflowEditorEventTypes.WorkflowDefinition.Imported, this.onWorkflowDefinitionImported);
    this.saveLabelsDebounced = debounce(this.saveWorkflowLabels, 1000);
  }

  @State() private createMode: boolean = false;

  @Method()
  async show() {
    await this.modalDialog.show();
  }

  @Method()
  async hide() {
    await this.modalDialog.hide();
  }

  async componentWillLoad() {
    this.elsaClient = await Container.get(ElsaApiClientProvider).getElsaClient();
    labelStore.labels = await this.labelsApi.list();
  }

  private saveWorkflowLabels = async () => {
    const labelIds = this.assignedLabelIds;
    let versionId = this.definitionVersionId;

    if (isNullOrWhitespace(versionId)) {
      let definition = await this.workflowEditor.getWorkflowDefinition();
      definition = await this.workflowDefinitionManager.saveWorkflow(definition, false);
      await this.workflowEditor.updateWorkflowDefinition(definition);
      versionId = definition.id;
      this.definitionVersionId = versionId;
    }

    await this.workflowDefinitionLabelsApi.update(versionId, labelIds);
  }

  private onWorkflowEditorReady = (e: WorkflowEditorReadyArgs) => {
    this.workflowEditor = e.workflowEditor;
  }

  private onToolbarDisplaying = (e: ToolbarDisplayingArgs) => {
    e.menu.menuItems.push({
      text: 'Labels',
      onClick: this.onLabelsMenuItemClicked,
      order: 3
    });
  }

  private onWorkflowPropertiesEditorDisplaying = (e: WorkflowPropertiesEditorDisplayingArgs) => {
    const propertiesTabModel = e.model.tabModels.find(x => x.name == 'properties') as PropertiesTabModel;

    propertiesTabModel.Widgets.push({
      name: 'labelPicker',
      content: () => {
        const assignedLabelIds = this.assignedLabelIds;
        return (
          <FormEntry label="Labels" fieldId="workflowLabels" hint="Labels allow you to tag the workflow that can be used to query workflows with.">
            <elsa-label-picker onSelectedLabelsChanged={this.onSelectedLabelsChanged} selectedLabels={assignedLabelIds}/>
          </FormEntry>);
      },
      order: 6
    });
  }

  private onWorkflowDefinitionImported = async (e: WorkflowDefinitionImportedArgs) => {
    const workflowDefinition = e.workflowDefinition;

    if (isNullOrWhitespace(workflowDefinition.id)) {
      this.definitionVersionId = null;
      this.assignedLabelIds = [];
      return;
    }

    this.assignedLabelIds = await this.workflowDefinitionLabelsApi.get(workflowDefinition.id);
    this.definitionVersionId = workflowDefinition.id;
  }

  private onLabelsMenuItemClicked = async () => await this.show();

  private onSelectedLabelsChanged = async (e: CustomEvent<Array<string>>) => {
    this.assignedLabelIds = e.detail;
    await this.saveLabelsDebounced();
  }

  render() {
    const labels = labelStore.labels;
    const createMode = this.createMode;
    const closeAction = DefaultActions.Close();
    const actions = [closeAction];

    return (
      <Host class="block">

        <elsa-modal-dialog ref={el => this.modalDialog = el} actions={actions}>
          <div class="pt-4">
            <h2 class="text-lg font-medium ml-4 mb-2">Labels</h2>

            <div class="pl-4 pr-6">
              <div class="flex justify-end">
                <div class="flex-shrink">
                  <button type="button" class="btn" onClick={() => this.createMode = !this.createMode}>New label</button>
                </div>
              </div>

              {createMode ? <elsa-label-creator onCreateLabelClicked={e => this.onCreateLabelClicked(e)}/> : undefined}

              <div class="mt-5">
                <div class="flex">
                  <div>
                    <p class="max-w-2xl text-sm text-gray-500">{labels.length == 1 ? '1 label' : `${labels.length} labels`}</p>
                  </div>
                </div>
                <div class="mt-5 border-t border-gray-200">
                  <div class="divide-y divide-gray-200">
                    {labels.map(label => <div class="border-top last:border-bottom border-solid border-gray-200">
                      <elsa-label-editor key={label.id}
                                         label={label}
                                         onLabelUpdated={e => this.onLabelUpdated(e)}
                                         onLabelDeleted={e => this.onLabelDeleted(e)}
                      />
                    </div>)}
                  </div>
                </div>
              </div>

            </div>
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }

  private createLabel = async (name: string, description?: string, color?: string): Promise<void> => {
    await this.labelsApi.create(name, description, color);
  }

  private updateLabel = async (id: string, name: string, description?: string, color?: string): Promise<void> => {
    await this.labelsApi.update(id, name, description, color);
  }

  private deleteLabel = async (id: string): Promise<void> => {
    await this.labelsApi.delete(id);
  }

  private async loadLabels() {
    labelStore.labels = await this.labelsApi.list();
  }

  private async refreshLabels() {
    await this.loadLabels();
    await this.eventBus.emit(EventTypes.Labels.Updated, this);
  }

  private async onCreateLabelClicked(e: CustomEvent<CreateLabelEventArgs>) {
    const args = e.detail;
    this.createMode = false;
    await this.createLabel(args.name, args.description, args.color);
    await this.refreshLabels();
  }

  private onLabelDeleted = async (e: CustomEvent<DeleteLabelEventArgs>) => {
    await this.deleteLabel(e.detail.id);
    await this.refreshLabels();
  }

  private onLabelUpdated = async (e: CustomEvent<UpdateLabelEventArgs>) => {
    const {id, name, description, color} = e.detail;
    await this.updateLabel(id, name, description, color);
    await this.refreshLabels();
  }
}

