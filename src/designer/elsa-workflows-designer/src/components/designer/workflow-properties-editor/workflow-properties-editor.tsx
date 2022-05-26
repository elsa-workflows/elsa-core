import {Component, Event, EventEmitter, h, Method, Prop, State, Watch} from '@stencil/core';
import WorkflowEditorTunnel from '../state';
import {TabChangedArgs, WorkflowDefinition} from '../../../models';
import {FormEntry} from "../../shared/forms/form-entry";
import {InfoList} from "../../shared/forms/info-list";
import {isNullOrWhitespace} from "../../../utils";
import {PropertiesTabModel, TabModel, WorkflowLabelsUpdatedArgs, WorkflowPropertiesEditorDisplayingArgs, WorkflowPropertiesEditorEventTypes, WorkflowPropertiesEditorModel, WorkflowPropsUpdatedArgs} from "./models";
import {Container} from "typedi";
import {EventBus} from "../../../services";

@Component({
  tag: 'elsa-workflow-properties-editor',
})
export class WorkflowPropertiesEditor {
  private readonly eventBus: EventBus;
  private readonly model: WorkflowPropertiesEditorModel;
  private slideOverPanel: HTMLElsaSlideOverPanelElement;

  constructor() {
    this.eventBus = Container.get(EventBus);

    this.model = {
      tabModels: [],
      refresh: this.refresh
    }
  }

  @Prop({mutable: true}) workflowDefinition?: WorkflowDefinition;
  @Event() workflowPropsUpdated: EventEmitter<WorkflowPropsUpdatedArgs>;
  @Event({bubbles: false}) workflowLabelsUpdated: EventEmitter<WorkflowLabelsUpdatedArgs>;
  @State() private selectedTabIndex: number = 0;
  @State() private changeHandle: object = {};

  @Method()
  public async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  public async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  @Watch('workflowDefinition')
  onWorkflowDefinitionChanged() {
    this.refresh();
  }

  async componentWillLoad() {
    const model = this.model;

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
            'Version': workflow.version,
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

    model.tabModels = [propertiesTabModel, variablesTabModel];

    const args: WorkflowPropertiesEditorDisplayingArgs = {model};

    await this.eventBus.emit(WorkflowPropertiesEditorEventTypes.Displaying, this, args);
  }

  public render() {
    const title = 'Workflow';
    const tabs = this.model.tabModels.map(x => x.tab);

    return (
      <elsa-form-panel
        headerText={title} tabs={tabs} selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}/>
    );
  }

  private refresh = () => {
    this.changeHandle = new Date();
  };

  private renderPropertiesTab = (tabModel: PropertiesTabModel) => {
    const widgets = tabModel.Widgets.sort((a, b) => a.order < b.order ? -1 : a.order > b.order ? 1 : 0);

    return <div>
      {widgets.map(widget => widget.content())}
    </div>
  };

  private renderVariablesTab = () => {
    return <div>
      TODO: Variables editor
    </div>
  };

  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex;

  private onPropertyEditorChanged = (apply: (w: WorkflowDefinition) => void) => {
    const workflowDefinition = this.workflowDefinition;
    apply(workflowDefinition);
    return this.workflowPropsUpdated.emit({workflowDefinition});
  }
}

WorkflowEditorTunnel.injectProps(WorkflowPropertiesEditor, ['activityDescriptors']);
