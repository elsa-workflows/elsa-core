import {Component, h, Listen, State} from "@stencil/core";
import {Container} from "typedi";
import {debounce} from 'lodash';
import labelStore from './label-store';
import {ElsaApiClientProvider, ElsaClient, EventBus} from "../../services";
import {ToolbarDisplayingArgs, ToolbarEventTypes} from "../../components/toolbar/workflow-toolbar-menu/models";
import {FormEntry} from "../../components/shared/forms/form-entry";
import {isNullOrWhitespace} from "../../utils";
import {WorkflowDefinitionManager} from "../../services/workflow-definition-manager";
import {LabelsApi, WorkflowDefinitionLabelsApi} from "./labels-api";
import {
  PropertiesTabModel,
  WorkflowDefinitionImportedArgs,
  WorkflowEditorEventTypes,
  WorkflowEditorReadyArgs,
  WorkflowPropertiesEditorDisplayingArgs,
  WorkflowPropertiesEditorEventTypes
} from "../../components/designer/workflow-definition-editor/models";

@Component({
  tag: 'elsa-labels-widget',
  shadow: false,
})
export class LabelsWidget {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private readonly labelsApi: LabelsApi;
  private readonly workflowDefinitionLabelsApi: WorkflowDefinitionLabelsApi;
  private readonly saveLabelsDebounced: () => void;
  private workflowEditor: HTMLElsaWorkflowDefinitionEditorElement;
  private labelsManager: HTMLElsaLabelsManagerElement;
  private elsaClient: ElsaClient;
  private definitionVersionId: string;

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

  private assignedLabelIds: Array<string> = [];

  public async componentWillLoad() {
    this.elsaClient = await Container.get(ElsaApiClientProvider).getElsaClient();
    labelStore.labels = await this.labelsApi.list();
  }

  public render() {
    return <elsa-labels-manager ref={el => this.labelsManager = el}/>
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

    const assignedLabelIds = await this.workflowDefinitionLabelsApi.get(workflowDefinition.id);
    this.assignedLabelIds = assignedLabelIds;
    this.definitionVersionId = workflowDefinition.id;
  }

  private onLabelsMenuItemClicked = async () => {
    await this.labelsManager.show();
  };

  private onSelectedLabelsChanged = async (e: CustomEvent<Array<string>>) => {
    this.assignedLabelIds = e.detail;
    await this.saveLabelsDebounced();
  }
}
