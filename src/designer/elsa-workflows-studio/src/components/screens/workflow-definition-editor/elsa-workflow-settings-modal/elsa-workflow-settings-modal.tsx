import {Component, Event, Host, Prop, State, Watch, h} from '@stencil/core';
import {eventBus} from "../../../../services/event-bus";
import {Map} from "../../../../utils/utils";
import {EventTypes, Variables, WorkflowContextFidelity, WorkflowContextOptions, WorkflowDefinition, WorkflowPersistenceBehavior} from "../../../../models";
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";
import {MarkerSeverity} from "monaco-editor";
import {checkBox, FormContext, selectField, SelectOption, textArea, textInput} from "../../../../utils/forms";
import {createElsaClient} from "../../../../services/elsa-client";

interface VariableDefinition {
  name?: string;
  value?: string
}

@Component({
  tag: 'elsa-workflow-settings-modal',
  shadow: false,
})
export class ElsaWorkflowDefinitionSettingsModal {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop() workflowDefinition: WorkflowDefinition;
  @State() workflowDefinitionInternal: WorkflowDefinition;
  @State() selectedTab: string = 'Settings';
  @State() newVariable: VariableDefinition = {};
  dialog: HTMLElsaModalDialogElement;
  monacoEditor: HTMLElsaMonacoElement;
  formContext: FormContext;
  workflowChannels: Array<string>;

  @Watch('workflowDefinition')
  handleWorkflowDefinitionChanged(newValue: WorkflowDefinition) {
    this.workflowDefinitionInternal = {...newValue};
    this.formContext = new FormContext(this.workflowDefinitionInternal, newValue => this.workflowDefinitionInternal = newValue);
  }

  async componentWillLoad() {
    this.handleWorkflowDefinitionChanged(this.workflowDefinition);

    const client = createElsaClient(this.serverUrl);
    this.workflowChannels = await client.workflowChannelsApi.list();
  }

  componentDidLoad() {
    eventBus.on(EventTypes.ShowWorkflowSettings, async () => await this.dialog.show(true));
  }

  onTabClick(e: Event, tab: string) {
    e.preventDefault();
    this.selectedTab = tab;
  }

  async onCancelClick() {
    await this.dialog.hide(true);
  }

  async onSubmit(e: Event) {
    e.preventDefault();
    await this.dialog.hide(true);
    setTimeout(() => eventBus.emit(EventTypes.UpdateWorkflowSettings, this, this.workflowDefinitionInternal), 250)
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    // Don't try and parse JSON if it contains errors.
    const errorCount = e.markers.filter(x => x.severity == MarkerSeverity.Error).length;

    if (errorCount > 0)
      return;

    const newValue = e.value;
    let data = this.workflowDefinitionInternal.variables ? this.workflowDefinitionInternal.variables.data || {} : {};

    try {
      data = newValue.indexOf('{') >= 0 ? JSON.parse(newValue) : {};
    } catch (e) {
    } finally {
      this.workflowDefinitionInternal = {...this.workflowDefinitionInternal, variables: {data: data}};
    }
  }

  render() {

    const tabs = ['Settings', 'Variables', 'Workflow Context', 'Advanced'];
    const selectedTab = this.selectedTab;
    const inactiveClass = 'elsa-border-transparent elsa-text-gray-500 hover:elsa-text-gray-700 hover:elsa-border-gray-300';
    const selectedClass = 'elsa-border-blue-500 elsa-text-blue-600';

    return (
      <Host>
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="elsa-py-8 elsa-pb-0">

            <form onSubmit={e => this.onSubmit(e)}>
              <div class="elsa-px-8 mb-8">
                <div class="elsa-border-b elsa-border-gray-200">
                  <nav class="-elsa-mb-px elsa-flex elsa-space-x-8" aria-label="Tabs">
                    {tabs.map(tab => {
                      const isSelected = tab === selectedTab;
                      const cssClass = isSelected ? selectedClass : inactiveClass;
                      return <a href="#" onClick={e => this.onTabClick(e, tab)} class={`${cssClass} elsa-whitespace-nowrap elsa-py-4 elsa-px-1 elsa-border-b-2 elsa-font-medium elsa-text-sm`}>{tab}</a>;
                    })}
                  </nav>
                </div>
              </div>

              {this.renderSelectedTab()}

              <div class="elsa-pt-5">
                <div class="elsa-bg-gray-50 elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
                  <button type="submit"
                          class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    Save
                  </button>
                  <button type="button"
                          onClick={() => this.onCancelClick()}
                          class="elsa-mt-3 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-white elsa-text-base elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-mt-0 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    Cancel
                  </button>
                </div>
              </div>
            </form>
          </div>

          <div slot="buttons"/>
        </elsa-modal-dialog>
      </Host>
    );
  }

  renderSelectedTab() {
    const selectedTab = this.selectedTab;

    switch (selectedTab) {
      case 'Workflow Context':
        return this.renderWorkflowContextTab();
      case 'Variables':
        return this.renderVariablesTab();
      case 'Advanced':
        return this.renderAdvancedTab();
      case 'Settings':
      default:
        return this.renderSettingsTab();
    }
  }

  renderSettingsTab() {
    const workflowDefinition = this.workflowDefinitionInternal;
    const formContext = this.formContext;

    return (
      <div class="elsa-flex elsa-px-8">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'name', 'Name', workflowDefinition.name, 'The technical name of the workflow.', 'workflowName')}
          {textInput(formContext, 'displayName', 'Display Name', workflowDefinition.displayName, 'A user-friendly display name of the workflow.', 'workflowDisplayName')}
          {textArea(formContext, 'description', 'Description', workflowDefinition.description, null, 'workflowDescription')}
        </div>
      </div>
    );
  }

  renderAdvancedTab() {
    const workflowDefinition = this.workflowDefinitionInternal;
    const formContext = this.formContext;
    const workflowChannelOptions: Array<SelectOption> = [{text: '', value: null}, ...this.workflowChannels.map(x => ({text: x, value: x}))];

    const persistenceBehaviorOptions: Array<SelectOption> = [{
      text: 'Suspended',
      value: 'Suspended'
    }, {
      text: 'Workflow Burst',
      value: 'WorkflowBurst'
    }, {
      text: 'Workflow Pass Completed',
      value: 'WorkflowPassCompleted'
    }, {
      text: 'Activity Executed',
      value: 'ActivityExecuted'
    }];

    return (
      <div class="elsa-flex elsa-px-8">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'tag', 'Tag', workflowDefinition.tag, 'Tags can be used to query workflow definitions with.', 'tag')}
          {selectField(formContext, 'persistenceBehavior', 'Persistence Behavior', workflowDefinition.persistenceBehavior, persistenceBehaviorOptions, 'The persistence behavior controls how often a workflow instance is persisted during workflow execution.', 'workflowContextFidelity')}
          {workflowChannelOptions.length > 0 ? selectField(formContext, 'channel', 'Channel', workflowDefinition.channel, workflowChannelOptions, 'Select a channel for this workflow to execute in.', 'channel') : undefined}
          {checkBox(formContext, 'isSingleton', 'Singleton', workflowDefinition.isSingleton, 'Singleton workflows will only have one active instance executing at a time.')}
        </div>
      </div>
    );
  }

  renderVariablesTab() {
    const workflowDefinition = this.workflowDefinitionInternal;
    const variables: Variables = workflowDefinition.variables || {data: {}};
    const data: Map<any> = variables.data || {};
    const value = JSON.stringify(data, undefined, 3);
    const language = 'json';

    return (
      <div class="elsa-flex elsa-px-8">
        <div class="elsa-space-y-8 elsa-w-full elsa-h-30">
          <elsa-monaco value={value} language={language} editor-height="30em" onValueChanged={e => this.onMonacoValueChanged(e.detail)} ref={el => this.monacoEditor = el}/>
        </div>
      </div>
    );
  }

  renderWorkflowContextTab() {
    const workflowDefinition = this.workflowDefinitionInternal;
    const formContext = this.formContext;

    const contextOptions: WorkflowContextOptions = workflowDefinition.contextOptions || {
      contextType: undefined,
      contextFidelity: WorkflowContextFidelity.Burst
    };

    const fidelityOptions: Array<SelectOption> = [{
      text: 'Burst',
      value: 'Burst'
    }, {
      text: 'Activity',
      value: 'Activity'
    }]

    return (
      <div class="elsa-flex elsa-px-8">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'contextOptions.contextType', 'Type', contextOptions.contextType, 'The fully qualified workflow context type name.', 'workflowContextType')}
          {selectField(formContext, 'contextOptions.contextFidelity', 'Fidelity', contextOptions.contextFidelity, fidelityOptions, 'The workflow context refresh fidelity controls the behavior of when to load and persist the workflow context.', 'workflowContextFidelity')}
        </div>
      </div>
    );
  }
}
