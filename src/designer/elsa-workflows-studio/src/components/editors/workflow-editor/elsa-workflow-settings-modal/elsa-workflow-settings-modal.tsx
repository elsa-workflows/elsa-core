import {Component, Event, h, Host, Prop, State, Watch} from '@stencil/core';
import {eventBus} from "../../../../services/event-bus";
import {Map} from "../../../../utils/utils";
import {EventTypes, Variables, WorkflowContextFidelity, WorkflowContextOptions, WorkflowDefinition} from "../../../../models";
import {MonacoValueChangedArgs} from "../../monaco/elsa-monaco/elsa-monaco";
import {MarkerSeverity} from "monaco-editor";

interface SelectOption {
  value: string;
  text: string;
}

interface VariableDefinition {
  name?: string;
  value?: string
}

@Component({
  tag: 'elsa-workflow-settings-modal',
  styleUrl: 'elsa-workflow-settings-modal.css',
  shadow: false,
})
export class ElsaWorkflowDefinitionSettingsModal {

  @Prop() workflowDefinition: WorkflowDefinition;
  @State() workflowDefinitionInternal: WorkflowDefinition;
  @State() selectedTab: string = 'Settings';
  @State() newVariable: VariableDefinition = {};
  dialog: HTMLElsaModalDialogElement;
  monacoEditor: HTMLElsaMonacoElement;

  @Watch('workflowDefinition')
  handleWorkflowDefinitionChanged(newValue: WorkflowDefinition) {
    this.workflowDefinitionInternal = {...newValue};
  }

  componentWillLoad() {
    this.handleWorkflowDefinitionChanged(this.workflowDefinition);
  }

  componentDidLoad() {
    eventBus.on(EventTypes.ShowWorkflowSettings, async () => await this.dialog.show(true));
  }

  // Supports hierarchical field names, e.g. 'foo.bar.baz`, which will map to e.g. { foo: { bar: { baz: ''} } }.
  updateField<T>(fieldName: string, value: T) {
    const model = {...this.workflowDefinitionInternal};
    const fieldNameHierarchy = fieldName.split('.');
    let current = model;

    for (const name of fieldNameHierarchy.slice(0, fieldNameHierarchy.length - 1)) {
      if (!current[name])
        current[name] = {};

      current = current[name];
    }

    const leafFieldName = fieldNameHierarchy.last();
    current[leafFieldName] = value;
    this.workflowDefinitionInternal = model;
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

  onTextInputChange(e: Event) {
    const element = e.target as HTMLInputElement;
    this.updateField(element.name, element.value.trim());
  }

  onTextAreaChange(e: Event) {
    const element = e.target as HTMLTextAreaElement;
    this.updateField(element.name, element.value.trim());
  }

  onCheckBoxChange(e: Event) {
    const element = e.target as HTMLInputElement;
    this.updateField(element.name, element.checked);
  }

  onSelectChange(e: Event) {
    const element = e.target as HTMLSelectElement;
    this.updateField(element.name, element.value.trim());
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

    const workflowDefinition = this.workflowDefinitionInternal;
    const tabs = ['Settings', 'Variables', 'Workflow Context'];
    const selectedTab = this.selectedTab;
    const inactiveClass = 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300';
    const selectedClass = 'border-blue-500 text-blue-600';

    return (
      <Host>
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="py-8 pb-0">

            <form onSubmit={e => this.onSubmit(e)}>
              <div class="px-8 mb-8">
                <div class="border-b border-gray-200">
                  <nav class="-mb-px flex space-x-8" aria-label="Tabs">
                    {tabs.map(tab => {
                      const isSelected = tab === selectedTab;
                      const cssClass = isSelected ? selectedClass : inactiveClass;
                      return <a href="#" onClick={e => this.onTabClick(e, tab)} class={`${cssClass} whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}>{tab}</a>;
                    })}
                  </nav>
                </div>
              </div>

              {this.renderSelectedTab()}

              <div class="pt-5">
                <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
                  <button type="submit"
                          class="ml-0 w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm">
                    Save
                  </button>
                  <button type="button"
                          onClick={() => this.onCancelClick()}
                          class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
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
      case 'Settings':
      default:
        return this.renderSettingsTab();
    }
  }

  renderSettingsTab() {
    const workflowDefinition = this.workflowDefinitionInternal;

    return (
      <div class="flex px-8">
        <div class="space-y-8 w-full">
          {this.textInput('name', 'Name', workflowDefinition.name, 'The technical name of the workflow.', 'workflowName')}
          {this.textInput('displayName', 'Display Name', workflowDefinition.displayName, 'A user-friendly display name of the workflow.', 'workflowDisplayName')}
          {this.textArea('description', 'Description', workflowDefinition.description, null, 'workflowDescription')}
          {this.checkBox('isSingleton', 'Singleton', workflowDefinition.isSingleton, 'Singleton workflows will only have one active instance executing at a time.')}
          {this.checkBox('isEnabled', 'Enabled', workflowDefinition.isEnabled, 'Check to enable this workflow.')}
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
      <div class="flex px-8">
        <div class="space-y-8 w-full h-30">
          <elsa-monaco value={value} language={language} editor-height="30em" onValueChanged={e => this.onMonacoValueChanged(e.detail)} ref={el => this.monacoEditor = el}/>
        </div>
      </div>
    );
  }

  renderWorkflowContextTab() {
    const workflowDefinition = this.workflowDefinitionInternal;

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
      <div class="flex px-8">
        <div class="space-y-8 w-full">
          {this.textInput('contextOptions.contextType', 'Type', contextOptions.contextType, 'The fully qualified workflow context type name.', 'workflowContextType')}
          {this.selectField('contextOptions.contextFidelity', 'Fidelity', contextOptions.contextFidelity.toString(), fidelityOptions, 'The workflow context refresh fidelity controls the behavior of when to load and persist the workflow context.', 'workflowContextFidelity')}
        </div>
      </div>
    );
  }

  textInput(fieldName: string, label: string, value: string, hint?: string, fieldId?: string) {
    fieldId = fieldId || fieldName
    return (
      <div>
        <label htmlFor={fieldName} class="block text-sm font-medium text-gray-700">
          {label}
        </label>
        <div class="mt-1">
          <input type="text" id={fieldId} name={fieldName} value={value} onChange={e => this.onTextInputChange(e)} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
        </div>
        {hint && hint.length > 0 ? <p class="mt-2 text-sm text-gray-500">{hint}</p> : undefined}
      </div>);
  }

  checkBox(fieldName: string, label: string, checked: boolean, hint?: string, fieldId?: string) {
    fieldId = fieldId || fieldName
    return (
      <div class="relative flex items-start">
        <div class="flex items-center h-5">
          <input id={fieldId} name={fieldName} type="checkbox" value="true" checked={checked} onChange={e => this.onCheckBoxChange(e)} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
        </div>
        <div class="ml-3 text-sm">
          <label htmlFor={fieldId} class="font-medium text-gray-700">{label}</label>
          {hint && hint.length > 0 ? <p class="text-gray-500">{hint}</p> : undefined}
        </div>
      </div>);
  }

  textArea(fieldName: string, label: string, value: string, hint?: string, fieldId?: string) {
    fieldId = fieldId || fieldName
    return (
      <div>
        <label htmlFor={fieldName} class="block text-sm font-medium text-gray-700">
          {label}
        </label>
        <div class="mt-1">
          <textarea id={fieldId} name={fieldName} value={value} onChange={e => this.onTextAreaChange(e)} rows={3} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
        </div>
        {hint && hint.length > 0 ? <p class="mt-2 text-sm text-gray-500">{hint}</p> : undefined}
      </div>);
  }

  selectField(fieldName: string, label: string, value: string, options: Array<SelectOption>, hint?: string, fieldId?: string) {
    fieldId = fieldId || fieldName
    return (
      <div>
        <label htmlFor={fieldName} class="block text-sm font-medium text-gray-700">
          {label}
        </label>
        <div class="mt-1">
          <select id={fieldId} name={fieldName} onChange={e => this.onSelectChange(e)} class="block focus:ring-blue-500 focus:border-blue-500 w-full shadow-sm sm:text-sm border-gray-300 rounded-md">
            {options.map(item => {
              const selected = item.value === value;
              return <option value={item.value} selected={selected}>{item.text}</option>;
            })}
          </select>
        </div>
        {hint && hint.length > 0 ? <p class="mt-2 text-sm text-gray-500">{hint}</p> : undefined}
      </div>);
  }
}
