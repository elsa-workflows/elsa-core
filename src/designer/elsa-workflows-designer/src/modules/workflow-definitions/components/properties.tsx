import {Component, Event, EventEmitter, h, Method, Prop, State, Watch} from '@stencil/core';
import {Container} from "typedi";
import {EventBus} from "../../../services";
import {InputDefinition, WorkflowDefinition, WorkflowOptions} from "../models/entities";
import {PropertiesTabModel, TabModel, Widget, WorkflowDefinitionPropsUpdatedArgs, WorkflowPropertiesEditorDisplayingArgs, WorkflowPropertiesEditorEventTypes, WorkflowPropertiesEditorModel} from "../models/ui";
import {FormEntry} from "../../../components/shared/forms/form-entry";
import {isNullOrWhitespace} from "../../../utils";
import {InfoList} from "../../../components/shared/forms/info-list";
import {TabChangedArgs, Variable, VersionedEntity} from "../../../models";
import {WorkflowDefinitionsApi} from "../services/api";
import descriptorsStore from "../../../data/descriptors-store";

@Component({
  tag: 'elsa-workflow-definition-properties-editor',
})
export class WorkflowDefinitionPropertiesEditor {
  private readonly eventBus: EventBus;
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private readonly workflowDefinitionApi: WorkflowDefinitionsApi;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.workflowDefinitionApi = Container.get(WorkflowDefinitionsApi);

    this.model = {
      tabModels: [],
    }
  }

  @Prop() workflowDefinition?: WorkflowDefinition;
  @Prop() workflowVersions: Array<WorkflowDefinition>;
  @Event() workflowPropsUpdated: EventEmitter<WorkflowDefinitionPropsUpdatedArgs>;
  @Event() versionSelected: EventEmitter<WorkflowDefinition>;
  @Event() deleteVersionClicked: EventEmitter<WorkflowDefinition>;
  @Event() revertVersionClicked: EventEmitter<WorkflowDefinition>;
  @State() private model: WorkflowPropertiesEditorModel;
  @State() private selectedTabIndex: number = 0;

  @Method()
  public async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  public async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged() {
    await this.createModel();
  }

  @Watch('workflowVersions')
  async onWorkflowVersionsChanged() {
    await this.createModel();
  }

  async componentWillLoad() {
    await this.createModel();
  }

  public render() {
    const workflowDefinition = this.workflowDefinition;
    const title = workflowDefinition?.name ?? 'Untitled';
    const subTitle = 'Workflow Definition'
    const tabs = this.model.tabModels.map(x => x.tab);

    return (
      <elsa-form-panel
        mainTitle={title}
        subTitle={subTitle}
        tabs={tabs}
        selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}/>
    );
  }

  private createModel = async () => {
    const model = {
      tabModels: []
    };

    const workflowDefinition = this.workflowDefinition;
    const options: WorkflowOptions = workflowDefinition.options || {};

    if (!workflowDefinition) {
      this.model = model;
      return;
    }

    const propertiesTabModel: PropertiesTabModel = {
      name: 'properties',
      tab: null,
      Widgets: [{
        name: 'workflowName',
        content: () => {
          const workflow = this.workflowDefinition;
          return <FormEntry label="Name" fieldId="workflowName" hint="The name of the workflow.">
            <input type="text" name="workflowName" id="workflowName" value={workflow.name} onChange={e => this.onPropertyEditorChanged(wf => wf.name = (e.target as HTMLInputElement).value)}/>
          </FormEntry>;
        },
        order: 0
      }, {
        name: 'workflowDescription',
        content: () => {
          const workflow = this.workflowDefinition;
          return <FormEntry label="Description" fieldId="workflowDescription" hint="A brief description about the workflow.">
            <textarea name="workflowDescription" id="workflowDescription" value={workflow.description} rows={6} onChange={e => this.onPropertyEditorChanged(wf => wf.description = (e.target as HTMLTextAreaElement).value)}/>
          </FormEntry>;
        },
        order: 5
      }, {
        name: 'workflowInfo',
        content: () => {
          const workflow = this.workflowDefinition;

          const workflowDetails = {
            'Definition ID': isNullOrWhitespace(workflow.definitionId) ? '(new)' : workflow.definitionId,
            'Version ID': isNullOrWhitespace(workflow.id) ? '(new)' : workflow.id,
            'Version': workflow.version.toString(),
            'Status': workflow.isPublished ? 'Published' : 'Draft'
          };

          return <InfoList title="Information" dictionary={workflowDetails}/>;
        },
        order: 10
      }]
    };

    propertiesTabModel.tab = {
      displayText: 'Properties',
      content: () => this.renderPropertiesTab(propertiesTabModel)
    };

    const variablesTabModel: TabModel = {
      name: 'variables',
      tab: {
        displayText: 'Variables',
        content: () => this.renderVariablesTab()
      }
    }

    const inputOutputTabModel: TabModel = {
      name: 'input-output',
      tab: {
        displayText: 'Arguments',
        content: () => this.renderInputOutputTab()
      }
    }

    const strategies = descriptorsStore.workflowActivationStrategyDescriptors;
    const firstStrategy = strategies.length > 0 ? strategies[0] : null;
    const defaultDescription = firstStrategy?.description ?? '';

    const settingsWidgets: Array<Widget> = [
      {
        name: 'workflowActivationValidator',
        order: 0,
        content: () => <FormEntry label="Activation Strategy" fieldId="workflowActivationStrategyType" hint={defaultDescription}>
          <select name="workflowActivationStrategyType" onChange={e => this.onPropertyEditorChanged(wf => {
            const selectElement = (e.target as HTMLSelectElement);
            options.activationStrategyType = selectElement.value;
            wf.options = options;

            const hintElement: HTMLElement = selectElement.closest('.form-entry').getElementsByClassName('form-field-hint')[0] as HTMLElement;
            const strategy = strategies.find(x => x.typeName == selectElement.value);
            hintElement.innerText = strategy.description;
          })}>
            {strategies.map(strategy => <option value={strategy.typeName} selected={strategy.typeName == options.activationStrategyType}>{strategy.displayName}</option>)}
          </select>
        </FormEntry>
      },
      {
        name: 'useAsActivity',
        order: 0,
        content: () => <FormEntry label="Usable As Activity" fieldId="useAsActivity" hint="Allow this workflow to be used as an activity.">
          <select name="workflowActivityFeature" onChange={e => this.onPropertyEditorChanged(wf => {
            const selectElement = (e.target as HTMLSelectElement);
            wf.usableAsActivity = selectElement.value != "false";
          })}>
            <option value="false" selected={!this.workflowDefinition.usableAsActivity}>No</option>
            <option value="true" selected={this.workflowDefinition.usableAsActivity}>Yes</option>
          </select>
        </FormEntry>
      }
    ];

    const settingsTabModel: TabModel = {
      name: 'settings',
      tab: {
        displayText: 'Settings',
        content: () => <elsa-widgets widgets={settingsWidgets}/>
      }
    }

    const versionHistoryTabModel: TabModel = {
      name: 'versionHistory',
      tab: {
        displayText: 'Version History',
        content: () => this.renderVersionHistoryTab()
      }
    }

    model.tabModels = [propertiesTabModel, variablesTabModel, inputOutputTabModel, settingsTabModel, versionHistoryTabModel];

    const args: WorkflowPropertiesEditorDisplayingArgs = {model};

    await this.eventBus.emit(WorkflowPropertiesEditorEventTypes.Displaying, this, args);

    this.model = model;
  }

  private renderPropertiesTab = (tabModel: PropertiesTabModel) => <elsa-widgets widgets={tabModel.Widgets}/>;

  private renderVariablesTab = () => {
    const variables: Array<Variable> = this.workflowDefinition?.variables ?? [];

    return <div>
      <elsa-variables-editor variables={variables} onVariablesChanged={e => this.onVariablesUpdated(e)}/>
    </div>
  };

  private renderInputOutputTab = () => {
    const inputs: Array<InputDefinition> = this.workflowDefinition?.inputs ?? [];

    return <div>
      <elsa-input-output-editor inputs={inputs} onInputsChanged={e => this.onInputsUpdated(e)}/>
    </div>
  };

  private renderVersionHistoryTab = () => {
    return <div>
      <elsa-workflow-definition-version-history
        selectedVersion={this.workflowDefinition}
        workflowVersions={this.workflowVersions}
      />
    </div>
  };

  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex;

  private onPropertyEditorChanged = (apply: (w: WorkflowDefinition) => void) => {
    const workflowDefinition = this.workflowDefinition;
    apply(workflowDefinition);
    this.workflowPropsUpdated.emit({workflowDefinition});
  }

  private onVariablesUpdated = async (e: CustomEvent<Array<Variable>>) => {
    const workflowDefinition = this.workflowDefinition;

    if (!workflowDefinition)
      return;

    workflowDefinition.variables = e.detail;
    this.workflowPropsUpdated.emit({workflowDefinition});
    await this.createModel();
  }

  private onInputsUpdated = async (e: CustomEvent<Array<InputDefinition>>) => {
    const workflowDefinition = this.workflowDefinition;

    if (!workflowDefinition)
      return;

    workflowDefinition.inputs = e.detail;
    this.workflowPropsUpdated.emit({workflowDefinition});
    await this.createModel();
  }
}
