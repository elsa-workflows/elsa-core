import {Component, Host, h, State, Event} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import state from '../../../../utils/store';
import {ActivityDescriptor, ActivityModel, ActivityPropertyDescriptor, EventTypes} from "../../../../models";
import {propertyDisplayManager} from '../../../../services/property-display-manager';
import {checkBox, FormContext, textArea, textInput} from "../../../../utils/forms";

@Component({
  tag: 'elsa-activity-editor-modal',
  styleUrl: 'elsa-activity-editor-modal.css',
  shadow: false,
})
export class ElsaActivityEditorModal {

  @State() activityModel: ActivityModel;
  @State() activityDescriptor: ActivityDescriptor;
  @State() selectedTab: string = 'Properties';
  dialog: HTMLElsaModalDialogElement;
  form: HTMLFormElement;
  formContext: FormContext;
  renderProps: any;
  
  // Force a new key every time we show the editor to make sure Stencil creates new components.
  // This prevents the issue where the designer has e.g. one activity where the user edits the properties, cancels out, then opens the editor again, seeing the entered value still there.
  timestamp: Date = new Date();

  componentDidLoad() {
    eventBus.on(EventTypes.ShowActivityEditor, async (activity: ActivityModel, animate: boolean) => {
      this.activityModel = JSON.parse(JSON.stringify(activity));
      this.activityDescriptor = state.activityDescriptors.find(x => x.type == activity.type);
      this.formContext = new FormContext(this.activityModel, newValue => this.activityModel = newValue);
      this.selectedTab = 'Properties'; 
      this.timestamp = new Date();
      await this.dialog.show(animate);
    });
  }

  updateActivity(formData: FormData) {
    const activity = this.activityModel;
    const activityDescriptor = this.activityDescriptor;
    const properties: Array<ActivityPropertyDescriptor> = activityDescriptor.properties;

    for (const property of properties)
      propertyDisplayManager.update(activity, property, formData);
  }

  async onCancelClick() {
    await this.dialog.hide(true);
  }

  async onSubmit(e: Event) {
    e.preventDefault();
    const form: any = e.target;
    const formData = new FormData(form);
    this.updateActivity(formData);
    eventBus.emit(EventTypes.UpdateActivity, this, this.activityModel);
    await this.dialog.hide(true);
  }

  onTabClick(e: Event, tab: string) {
    e.preventDefault();
    this.selectedTab = tab;
  }

  componentWillRender(){
    const activityDescriptor = this.activityDescriptor || {displayName: '', type: '', outcomes: [], category: '', traits: 0, browsable: false, properties: [], description: ''};
    const propertyCategories = activityDescriptor.properties.filter(x => x.category).map(x => x.category).distinct();
    const defaultProperties = activityDescriptor.properties.filter(x => !x.category || x.category.length == 0);
    let tabs: Array<string> = [];

    if(defaultProperties.length > 0) {
      tabs.push('Properties');
    }

    tabs.push('Common');
    tabs.push('Behaviors');
    tabs = [...tabs, ...propertyCategories];
    let selectedTab = this.selectedTab;

    if(tabs.findIndex(x => x === selectedTab) < 0)
      selectedTab = tabs[0];

    const activityModel: ActivityModel = this.activityModel || {type: '', activityId: '', outcomes: [], properties: []};

    this.renderProps = {
      activityDescriptor,
      propertyCategories,
      defaultProperties,
      tabs,
      selectedTab,
      activityModel
    }
  }

  render() {
    const renderProps = this.renderProps;
    const activityDescriptor = renderProps.activityDescriptor;
    const propertyCategories = renderProps.propertyCategories;
    const tabs = renderProps.tabs;
    const selectedTab = renderProps.selectedTab;
    const activityModel: ActivityModel = renderProps.activityModel;
    const inactiveClass = 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300';
    const selectedClass = 'border-blue-500 text-blue-600';

    return (
      <Host>
        <elsa-modal-dialog ref={el => this.dialog = el} >
          <div slot="content" class="py-8 pb-0">
            <form onSubmit={e => this.onSubmit(e)} ref={el => this.form = el} key={this.timestamp.getTime().toString()}>
              <div class="flex px-8">
                <div class="space-y-8 divide-y divide-gray-200 w-full">
                  <div>
                    <div>
                      <h3 class="text-lg leading-6 font-medium text-gray-900">
                        {activityDescriptor.displayName}
                      </h3>
                      <p class="mt-1 text-sm text-gray-500">
                        {activityDescriptor.description}
                      </p>
                    </div>

                    <div class="border-b border-gray-200">
                      <nav class="-mb-px flex space-x-8" aria-label="Tabs">
                        {tabs.map(tab => {
                          const isSelected = tab === selectedTab;
                          const cssClass = isSelected ? selectedClass : inactiveClass;
                          return <a href="#" onClick={e => this.onTabClick(e, tab)} class={`${cssClass} whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}>{tab}</a>;
                        })}
                      </nav>
                    </div>

                    <div class="mt-8">
                      {this.renderSelectedTab(activityModel, activityDescriptor, propertyCategories)}
                    </div>
                  </div>

                </div>
              </div>
              <div class="pt-5">
                <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
                  <button type="submit"
                          class="ml-3 inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
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

  renderSelectedTab(activityModel: ActivityModel, activityDescriptor: ActivityDescriptor, categories: Array<string>) {
    return [
      this.renderWorkflowContextTab(activityModel),
      this.renderCommonTab(activityModel),
      this.renderPropertiesTab(activityModel, activityDescriptor),
      this.renderCategoryTabs(activityModel, activityDescriptor, categories)
    ];
  }

  renderWorkflowContextTab(activityModel: ActivityModel) {
    const formContext = this.formContext;

    return (
      <div class={`flex ${this.getHiddenClass('Behaviors')}`}>
        <div class="space-y-8 w-full">
          {checkBox(formContext, 'loadWorkflowContext', 'Load Workflow Context', activityModel.loadWorkflowContext, 'When enabled, this will load the workflow context into memory before executing this activity.', 'loadWorkflowContext')}
          {checkBox(formContext, 'saveWorkflowContext', 'Save Workflow Context', activityModel.saveWorkflowContext, 'When enabled, this will save the workflow context back into storage after executing this activity.', 'saveWorkflowContext')}
          {checkBox(formContext, 'persistWorkflow', 'Save Workflow Instance', activityModel.persistWorkflow, 'When enabled, this will save the workflow instance back into storage right after executing this activity.', 'persistWorkflow')}
          {checkBox(formContext, 'persistOutput', 'Save Activity Output', activityModel.persistOutput, 'When enabled, this will store this activity\'s output as part of the workflow instance. Enable this when you plan to reference this output from other activities', 'persistOutput')}
        </div>
      </div>
    );
  }

  renderCommonTab(activityModel: ActivityModel) {
    const formContext = this.formContext;

    return (
      <div class={`flex ${this.getHiddenClass('Common')}`}>
        <div class="space-y-8 w-full">
          {textInput(formContext, 'name', 'Name', activityModel.name, 'The technical name of the activity.', 'activityName')}
          {textInput(formContext, 'displayName', 'Display Name', activityModel.displayName, 'A friendly name of the activity.', 'activityDisplayName')}
          {textArea(formContext, 'description', 'Description', activityModel.description, 'A custom description for this activity', 'activityDescription')}
        </div>
      </div>
    );
  }

  renderPropertiesTab(activityModel: ActivityModel, activityDescriptor: ActivityDescriptor) {
    const propertyDescriptors: Array<ActivityPropertyDescriptor> = this.renderProps.defaultProperties;

    if(propertyDescriptors.length == 0)
      return undefined;

    const key = `activity-settings:${activityModel.activityId}`;

    return (
      <div key={key} class={`grid grid-cols-1 gap-y-6 gap-x-4 sm:grid-cols-6 ${this.getHiddenClass('Properties')}`}>
        {propertyDescriptors.map(property => this.renderPropertyEditor(activityModel, property))}
      </div>
    );
  }

  renderCategoryTabs(activityModel: ActivityModel, activityDescriptor: ActivityDescriptor, categories: Array<string>) {
    const propertyDescriptors: Array<ActivityPropertyDescriptor> = activityDescriptor.properties;

    return (
      categories.map(category => {
        const descriptors = propertyDescriptors.filter(x => x.category == category);
        const key = `activity-settings:${activityModel.activityId}:${category}`;
        return <div key={key} class={`grid grid-cols-1 gap-y-6 gap-x-4 sm:grid-cols-6 ${this.getHiddenClass(category)}`}>
          {descriptors.map(property => this.renderPropertyEditor(activityModel, property))}
        </div>
      })
    );
  }

  renderPropertyEditor(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const key = `activity-property-input:${activity.activityId}:${property.name}`;
    return <div key={key} class="sm:col-span-6">{propertyDisplayManager.display(activity, property)}</div>;
  }

  getHiddenClass(tab: string) {
    return this.renderProps.selectedTab == tab ? '' : 'hidden';
  }
}
