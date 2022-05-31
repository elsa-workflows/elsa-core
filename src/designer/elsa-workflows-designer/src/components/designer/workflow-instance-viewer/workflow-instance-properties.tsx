import {Component, Event, EventEmitter, h, Method, Prop, State, Watch} from '@stencil/core';
import WorkflowEditorTunnel from '../state';
import {TabChangedArgs, WorkflowDefinition, WorkflowInstance} from '../../../models';
import {InfoList} from "../../shared/forms/info-list";
import {formatTimestamp, isNullOrWhitespace} from "../../../utils";
import {PropertiesTabModel, TabModel, WorkflowInstancePropertiesDisplayingArgs, WorkflowInstancePropertiesEventTypes, WorkflowInstancePropertiesViewerModel} from "./models";
import {Container} from "typedi";
import {EventBus} from "../../../services";

@Component({
  tag: 'elsa-workflow-instance-properties',
})
export class WorkflowDefinitionPropertiesEditor {
  private readonly eventBus: EventBus;
  private slideOverPanel: HTMLElsaSlideOverPanelElement;

  constructor() {
    this.eventBus = Container.get(EventBus);

    this.model = {
      tabModels: [],
    }
  }

  @Prop() workflowDefinition?: WorkflowDefinition;
  @Prop() workflowInstance?: WorkflowInstance;
  @State() private model: WorkflowInstancePropertiesViewerModel;
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
  async onWorkflowDefinitionChanged() {
    await this.createModel();
  }

  @Watch('workflowInstance')
  async onWorkflowInstanceChanged() {
    await this.createModel();
  }

  async componentWillLoad() {
    await this.createModel();
  }

  public render() {
    const workflowDefinition = this.workflowDefinition;
    const title = !isNullOrWhitespace(workflowDefinition?.name) ? workflowDefinition.name : '-';
    const subTitle='Workflow Instance';
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
      tabModels: [],
    };

    const workflowDefinition = this.workflowDefinition;
    const workflowInstance = this.workflowInstance;

    if (!workflowDefinition || !workflowInstance) {
      this.model = model;
      return;
    }

    const propertiesTabModel: PropertiesTabModel = {
      name: 'properties',
      tab: null,
      Widgets: [{
        name: 'workflowInstanceInfo',
        content: () => {

          const identityDetails = {
            'Instance ID': isNullOrWhitespace(workflowInstance.id) ? '(new)' : workflowInstance.id,
            'Definition ID': isNullOrWhitespace(workflowDefinition.definitionId) ? '(new)' : workflowDefinition.definitionId,
            'Version': workflowDefinition.version.toString(),
          };

          const statusDetails = {
            'Status': workflowInstance.status,
            'Sub status': workflowInstance.subStatus
          };

          const timestampDetails = {
            'Created': formatTimestamp(workflowInstance.createdAt),
            'Last execution': formatTimestamp(workflowInstance.lastExecutedAt),
            'Finished': formatTimestamp(workflowInstance.finishedAt),
            'Faulted': formatTimestamp(workflowInstance.faultedAt),
          };

          return <div>
            <InfoList title="Identity" dictionary={identityDetails}/>
            <InfoList title="Status" dictionary={statusDetails}/>
            <InfoList title="Timestamps" dictionary={timestampDetails} hideEmptyValues={true}/>
          </div>;
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

    const args: WorkflowInstancePropertiesDisplayingArgs = {model};

    await this.eventBus.emit(WorkflowInstancePropertiesEventTypes.Displaying, this, args);

    this.model = model;
  }

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
}

WorkflowEditorTunnel.injectProps(WorkflowDefinitionPropertiesEditor, ['activityDescriptors']);
