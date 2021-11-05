import {Component, Host, Prop, State, Watch, h, Listen} from '@stencil/core';
import {eventBus} from '../../../../services';
import {Map} from "../../../../utils/utils";
import {
  EventTypes,
  Variables,
  WorkflowContextFidelity,
  WorkflowContextOptions,
  WorkflowDefinition,
  ActivityModel,
  ActivityPropertyDescriptor,
} from "../../../../models";
import {MonacoValueChangedArgs} from "../../../controls/elsa-monaco/elsa-monaco";
import {MarkerSeverity} from "monaco-editor";
import {checkBox, FormContext, selectField, SelectOption, textArea, textInput} from "../../../../utils/forms";
import {createElsaClient} from "../../../../services/elsa-client";
import { createElsaWorkflowSettingsClient } from '../../../../modules/elsa-workflows-settings/services/elsa-client';
import { WorkflowDefinitionProperty } from '../../../../modules/elsa-workflows-settings/models';
import { ValidationStatus, WorkflowDefinitionPropertyValidationErrors } from '../../../../validation/workflow-definition-property-validation/workflow-definition-property.messages';

import { forOwn } from "lodash"

interface WorkflowTabModel {
  tabName: string;
  renderContent: () => any;
}

export interface WorkflowSettingsRenderProps {
  workflowDefinition?: WorkflowDefinition;
  tabs?: Array<WorkflowTabModel>;
  selectedTabName?: string;
  properties?: Array<WorkflowDefinitionProperty>;
  propertiesToRemove?: Array<WorkflowDefinitionProperty>;
  validationErrors?: WorkflowDefinitionPropertyValidationErrors;
}

@Component({
  tag: 'elsa-workflow-settings-modal',
  shadow: false,
})
export class ElsaWorkflowDefinitionSettingsModal {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop() workflowDefinition: WorkflowDefinition;
  @State() renderProps: WorkflowSettingsRenderProps = {}
  @State() validation: Array<ValidationStatus> = [];

  dialog: HTMLElsaModalDialogElement;
  monacoEditor: HTMLElsaMonacoElement;
  formContext: FormContext;
  workflowChannels: Array<string>;
  activityModel: ActivityModel;
  propertyDescriptor: ActivityPropertyDescriptor;

  propertiesInternal: Array<WorkflowDefinitionProperty>;

  @Watch('workflowDefinition')
  handleWorkflowDefinitionChanged(newValue: WorkflowDefinition) {
    this.renderProps.workflowDefinition = {...newValue};
    this.formContext = new FormContext(this.renderProps.workflowDefinition, newValue => this.renderProps.workflowDefinition = newValue);
  }

  async componentWillLoad() {
    this.handleWorkflowDefinitionChanged(this.workflowDefinition);
    
    const client = await createElsaClient(this.serverUrl);
    this.workflowChannels = await client.workflowChannelsApi.list();

    await this.loadProperties();
    eventBus.on(EventTypes.WorkflowSettingsDeleted, this.loadProperties);
    eventBus.on(EventTypes.WorkflowPropertiesValidationChanged, this.renderValidationErrors);
  }

  async componentWillRender() {
    let tabs: Array<WorkflowTabModel> = [
    {
      tabName: 'Settings', 
      renderContent: () => this.renderSettingsTab(this.renderProps.workflowDefinition)
    }, 
    {
      tabName: 'Variables',
      renderContent: () => this.renderVariablesTab(this.renderProps.workflowDefinition)
    }, 
    {
      tabName: 'Workflow Context',
      renderContent: () => this.renderWorkflowContextTab(this.renderProps.workflowDefinition)
    },
    {
      tabName: 'Advanced',
      renderContent: () => this.renderAdvancedTab(this.renderProps.workflowDefinition)
    }];

    const renderProps: WorkflowSettingsRenderProps = {
      workflowDefinition: this.workflowDefinition,
      properties: this.propertiesInternal,
      tabs,
      selectedTabName: this.renderProps.selectedTabName
    };

    await eventBus.emit(EventTypes.WorkflowSettingsModalLoaded, this, renderProps)
    this.renderProps = renderProps;

    let selectedTabName = this.renderProps.selectedTabName
    tabs = this.renderProps.tabs;

    if (!selectedTabName)
      this.renderProps.selectedTabName = tabs[0].tabName;

    if (tabs.findIndex(x => x.tabName === selectedTabName) < 0)
      this.renderProps.selectedTabName = selectedTabName = tabs[0].tabName;
  }

  componentDidLoad() {
    eventBus.on(EventTypes.ShowWorkflowSettings, async () => await this.dialog.show(true));
  }

  onTabClick = (e: Event, tab: WorkflowTabModel) => {
    e.preventDefault();
    this.renderProps = {...this.renderProps, selectedTabName: tab.tabName};
  };

  async onCancelClick() {
    eventBus.emit(EventTypes.WorkflowSettingsClosing, this, this.renderProps.properties); 
    this.validation = [];
    await this.dialog.hide(true);
  }

  async onSubmit(e: Event) {
    e.preventDefault();
    await this.dialog.hide(true);

    setTimeout(() => {
      eventBus.emit(EventTypes.UpdateWorkflowSettings, this, this.renderProps.workflowDefinition);
      eventBus.emit(EventTypes.WorkflowSettingsUpdaing, this, this.renderProps.workflowDefinition.settings); 
      if(this.renderProps.propertiesToRemove && this.renderProps.propertiesToRemove.length)
        eventBus.emit(EventTypes.WorkflowSettingsBulkDelete, this, this.renderProps.propertiesToRemove.map(x => x.id));
    }, 250)
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    // Don't try and parse JSON if it contains errors.
    const errorCount = e.markers.filter(x => x.severity == MarkerSeverity.Error).length;

    if (errorCount > 0)
      return;

    const newValue = e.value;
    let data = this.renderProps.workflowDefinition.variables ? this.renderProps.workflowDefinition.variables.data || {} : {};

    try {
      data = newValue.indexOf('{') >= 0 ? JSON.parse(newValue) : {};
    } catch (e) {
    } finally {
      this.renderProps.workflowDefinition = {...this.renderProps.workflowDefinition, variables: {data: data}};
    }
  }

  async loadProperties() {
    const workflowSettingsClient = await createElsaWorkflowSettingsClient('https://localhost:11000');
    const properties: WorkflowDefinitionProperty[] = await workflowSettingsClient.workflowSettingsApi.list();

    this.propertiesInternal = properties;
}

  render() {
    const renderProps = this.renderProps;
    const tabs = renderProps.tabs;
    const selectedTabName = renderProps.selectedTabName;
    const validation = this.validation;
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
                          const isSelected = tab.tabName === selectedTabName;
                          const cssClass = isSelected ? selectedClass : inactiveClass;
                          return <a href="#" onClick={e => this.onTabClick(e, tab)}
                                    class={`${cssClass} elsa-whitespace-nowrap elsa-py-4 elsa-px-1 elsa-border-b-2 elsa-font-medium elsa-text-sm`}>{tab.tabName}</a>;
                        })}
                  </nav>
                </div>

                <div>
                  {this.renderTabs(tabs)}
                </div>
                <div class="elsa-pt-5">
                {validation.map(validation => {
                    return <div class="elsa-text-red-800 elsa-text-xs">{validation.message}</div>
                  })
                }
                </div>
              </div>
              <div class="elsa-pt-5">
                <div class="elsa-bg-gray-50 elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
                  { validation.find(x => !x.valid) ? 
                  <button type="submit"
                          disabled={true}
                          class="disabled:elsa-opacity-50 elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    Save
                  </button> : 
                  <button type="submit"
                          class="elsa-ml-0 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-transparent elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-blue-600 elsa-text-base elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    Save
                  </button>
                  }
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

  renderTabs(tabs: Array<WorkflowTabModel>) {
    return tabs.map(x =>
      (
        <div class={`flex ${this.getHiddenClass(x.tabName)}`}>
          <elsa-control content={x.renderContent()}/>
        </div>
      ));
  }

  renderSettingsTab(workflowDefinition: WorkflowDefinition) {
    const formContext = this.formContext;

    return (
      <div class="elsa-flex">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'name', 'Name', workflowDefinition.name, 'The technical name of the workflow.', 'workflowName')}
          {textInput(formContext, 'displayName', 'Display Name', workflowDefinition.displayName, 'A user-friendly display name of the workflow.', 'workflowDisplayName')}
          {textArea(formContext, 'description', 'Description', workflowDefinition.description, null, 'workflowDescription')}
        </div>
      </div>
    );
  }

  renderAdvancedTab(workflowDefinition: WorkflowDefinition) {
    const formContext = this.formContext;
    const workflowChannelOptions: Array<SelectOption> = [{
      text: '',
      value: null
    }, ...this.workflowChannels.map(x => ({text: x, value: x}))];

    const persistenceBehaviorOptions: Array<SelectOption> = [{
      text: 'Suspended',
      value: 'Suspended'
    }, {
      text: 'Workflow Burst',
      value: 'WorkflowBurst'
    }, {
      text: 'Activity Executed',
      value: 'ActivityExecuted'
    }];

    return (
      <div class="elsa-flex">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'tag', 'Tag', workflowDefinition.tag, 'Tags can be used to query workflow definitions with.', 'tag')}
          {selectField(formContext, 'persistenceBehavior', 'Persistence Behavior', workflowDefinition.persistenceBehavior, persistenceBehaviorOptions, 'The persistence behavior controls how often a workflow instance is persisted during workflow execution.', 'workflowContextFidelity')}
          {workflowChannelOptions.length > 0 ? selectField(formContext, 'channel', 'Channel', workflowDefinition.channel, workflowChannelOptions, 'Select a channel for this workflow to execute in.', 'channel') : undefined}
          {checkBox(formContext, 'isSingleton', 'Singleton', workflowDefinition.isSingleton, 'Singleton workflows will only have one active instance executing at a time.')}
        </div>
      </div>
    );
  }

  renderVariablesTab(workflowDefinition: WorkflowDefinition) {
    const variables: Variables = workflowDefinition.variables || {data: {}};
    const data: Map<any> = variables.data || {};
    const value = JSON.stringify(data, undefined, 3);
    const language = 'json';

    return (
      <div class="elsa-flex">
        <div class="elsa-space-y-8 elsa-w-full elsa-h-30">
          <elsa-monaco value={value} language={language} editor-height="30em"
                       onValueChanged={e => this.onMonacoValueChanged(e.detail)} ref={el => this.monacoEditor = el}/>
        </div>
      </div>
    );
  }

  renderWorkflowContextTab(workflowDefinition: WorkflowDefinition) {
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
      <div class="elsa-flex">
        <div class="elsa-space-y-8 elsa-w-full">
          {textInput(formContext, 'contextOptions.contextType', 'Type', contextOptions.contextType, 'The fully qualified workflow context type name.', 'workflowContextType')}
          {selectField(formContext, 'contextOptions.contextFidelity', 'Fidelity', contextOptions.contextFidelity, fidelityOptions, 'The workflow context refresh fidelity controls the behavior of when to load and persist the workflow context.', 'workflowContextFidelity')}
        </div>
      </div>
    );
  }

  renderValidationErrors(validationErrors: WorkflowDefinitionPropertyValidationErrors) {
    this.validation = [];
  
    forOwn(validationErrors, (value, key) => { 
      if(key === 'PropertyKeyNameError' && value && value.length) {
        let propertyKeyNameError = value.find(x => !x.validation.valid);
        this.validation[this.validation.length] = propertyKeyNameError ? propertyKeyNameError.validation : value[0].validation
      } else {
        this.validation[this.validation.length] = value ? value : null
      }
    });
  }

  getHiddenClass(tab: string) {
    return this.renderProps.selectedTabName === tab ? '' : 'hidden';
  }
}
