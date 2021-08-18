import {Component, Event, h, Host, Prop, State} from '@stencil/core';
import {eventBus, propertyDisplayManager} from '../../../../services';
import state from '../../../../utils/store';
import {ActivityDescriptor, ActivityModel, ActivityPropertyDescriptor, EventTypes, WorkflowStorageDescriptor} from "../../../../models";
import {checkBox, FormContext, section, selectField, SelectOption, textArea, textInput} from "../../../../utils/forms";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";

export interface TabModel {
  tabName: string;
  renderContent: () => any;
}

export interface ActivityEditorRenderProps {
  activityDescriptor?: ActivityDescriptor;
  activityModel?: ActivityModel;
  propertyCategories?: Array<string>;
  defaultProperties?: Array<ActivityPropertyDescriptor>;
  tabs?: Array<TabModel>;
  selectedTabName?: string;
}

@Component({
  tag: 'elsa-activity-editor-modal',
  shadow: false,
})
export class ElsaActivityEditorModal {
  @Prop() culture: string;
  @State() workflowStorageDescriptors: Array<WorkflowStorageDescriptor> = [];
  @State() activityModel: ActivityModel;
  @State() activityDescriptor: ActivityDescriptor;
  @State() renderProps: ActivityEditorRenderProps = {};
  i18next: i18n;
  dialog: HTMLElsaModalDialogElement;
  form: HTMLFormElement;
  formContext: FormContext;

  // Force a new key every time we show the editor to make sure Stencil creates new components.
  // This prevents the issue where the designer has e.g. one activity where the user edits the properties, cancels out, then opens the editor again, seeing the entered value still there.
  timestamp: Date = new Date();

  connectedCallback() {
    eventBus.on(EventTypes.ShowActivityEditor, this.onShowActivityEditor);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ShowActivityEditor, this.onShowActivityEditor);
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  t = (key: string) => this.i18next.t(key);

  updateActivity(formData: FormData) {
    const activity = this.activityModel;
    const activityDescriptor = this.activityDescriptor;
    const inputProperties: Array<ActivityPropertyDescriptor> = activityDescriptor.inputProperties;

    for (const property of inputProperties)
      propertyDisplayManager.update(activity, property, formData);
  }

  componentWillRender() {
    const activityDescriptor: ActivityDescriptor = this.activityDescriptor || {displayName: '', type: '', outcomes: [], category: '', traits: 0, browsable: false, inputProperties: [], outputProperties: [], description: ''};
    const propertyCategories = activityDescriptor.inputProperties.filter(x => x.category).map(x => x.category).distinct();
    const defaultProperties = activityDescriptor.inputProperties.filter(x => !x.category || x.category.length == 0);
    const activityModel: ActivityModel = this.activityModel || {type: '', activityId: '', outcomes: [], properties: [], propertyStorageProviders: {}};
    const t = this.t;
    let tabs: Array<TabModel> = [];

    if (defaultProperties.length > 0) {
      tabs.push({
        tabName: t('Tabs.Properties.Name'),
        renderContent: () => this.renderPropertiesTab(activityModel)
      });
    }

    for (const category of propertyCategories) {
      const categoryTab: TabModel = {
        tabName: category,
        renderContent: () => this.renderCategoryTab(activityModel, activityDescriptor, category)
      };

      tabs.push(categoryTab);
    }

    tabs.push({
      tabName: t('Tabs.Common.Name'),
      renderContent: () => this.renderCommonTab(activityModel)
    });

    tabs.push({
      tabName: t('Tabs.Storage.Name'),
      renderContent: () => this.renderStorageTab(activityModel, activityDescriptor)
    });

    this.renderProps = {
      activityDescriptor,
      activityModel,
      propertyCategories,
      defaultProperties,
      tabs,
      selectedTabName: this.renderProps.selectedTabName
    };

    eventBus.emit(EventTypes.ActivityEditorDisplaying, this, this.renderProps);

    let selectedTabName = this.renderProps.selectedTabName
    tabs = this.renderProps.tabs;

    if (!selectedTabName)
      this.renderProps.selectedTabName = tabs[0].tabName;

    if (tabs.findIndex(x => x.tabName === selectedTabName) < 0)
      this.renderProps.selectedTabName = selectedTabName = tabs[0].tabName;
  }

  async onCancelClick() {
    await this.dialog.hide(true);
  }

  onSubmit = async (e: Event) => {
    e.preventDefault();
    const form: any = e.target;
    const formData = new FormData(form);
    this.updateActivity(formData);
    eventBus.emit(EventTypes.UpdateActivity, this, this.activityModel);
    await this.dialog.hide(true);
  };

  onTabClick = (e: Event, tab: TabModel) => {
    e.preventDefault();
    this.renderProps = {...this.renderProps, selectedTabName: tab.tabName};
  };

  onShowActivityEditor = async (activity: ActivityModel, animate: boolean) => {
    const t = this.t;
    this.activityModel = JSON.parse(JSON.stringify(activity));
    this.activityDescriptor = state.activityDescriptors.find(x => x.type == activity.type);
    this.workflowStorageDescriptors = state.workflowStorageDescriptors;
    this.formContext = new FormContext(this.activityModel, newValue => this.activityModel = newValue);
    this.timestamp = new Date();
    this.renderProps = {};
    await this.dialog.show(animate);
  };

  render() {
    const renderProps = this.renderProps;
    const activityDescriptor: ActivityDescriptor = renderProps.activityDescriptor;
    const tabs = renderProps.tabs;
    const selectedTabName = renderProps.selectedTabName;
    const inactiveClass = 'elsa-border-transparent elsa-text-gray-500 hover:elsa-text-gray-700 hover:elsa-border-gray-300';
    const selectedClass = 'elsa-border-blue-500 elsa-text-blue-600';
    const t = this.t;

    return (
      <Host class="elsa-block">
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="elsa-py-8 elsa-pb-0">
            <form onSubmit={e => this.onSubmit(e)} ref={el => this.form = el} key={this.timestamp.getTime().toString()}>
              <div class="elsa-flex elsa-px-8">
                <div class="elsa-space-y-8 elsa-divide-y elsa-divide-gray-200 elsa-w-full">
                  <div>
                    <div>
                      <h3 class="elsa-text-lg elsa-leading-6 elsa-font-medium elsa-text-gray-900">
                        {activityDescriptor.type}
                      </h3>
                      <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
                        {activityDescriptor.description}
                      </p>
                    </div>

                    <div class="elsa-border-b elsa-border-gray-200">
                      <nav class="-elsa-mb-px elsa-flex elsa-space-x-8" aria-label="Tabs">
                        {tabs.map(tab => {
                          const isSelected = tab.tabName === selectedTabName;
                          const cssClass = isSelected ? selectedClass : inactiveClass;
                          return <a href="#" onClick={e => this.onTabClick(e, tab)} class={`${cssClass} elsa-whitespace-nowrap elsa-py-4 elsa-px-1 elsa-border-b-2 elsa-font-medium elsa-text-sm`}>{tab.tabName}</a>;
                        })}
                      </nav>
                    </div>

                    <div class="elsa-mt-8">
                      {this.renderTabs(tabs)}
                    </div>
                  </div>

                </div>
              </div>
              <div class="elsa-pt-5">
                <div class="elsa-bg-gray-50 elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
                  <button type="submit"
                          class="elsa-ml-3 elsa-inline-flex elsa-justify-center elsa-py-2 elsa-px-4 elsa-border elsa-border-transparent elsa-shadow-sm elsa-text-sm elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-blue-600 hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500">
                    {t('Buttons.Save')}
                  </button>
                  <button type="button"
                          onClick={() => this.onCancelClick()}
                          class="elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-white elsa-text-base elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-mt-0 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                    {t('Buttons.Cancel')}
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

  renderTabs(tabs: Array<TabModel>) {
    return tabs.map(x =>
      (
        <div class={`flex ${this.getHiddenClass(x.tabName)}`}>
          <elsa-control content={x.renderContent()}/>
        </div>
      ));
  }

  renderStorageTab(activityModel: ActivityModel, activityDescriptor: ActivityDescriptor) {
    const formContext = this.formContext;
    const t = this.t;
    let storageDescriptorOptions: Array<SelectOption> = this.workflowStorageDescriptors.map(x => ({value: x.name, text: x.displayName}));
    let outputProperties = activityDescriptor.outputProperties.filter(x => !x.disableWorkflowProviderSelection);
    let inputProperties = activityDescriptor.inputProperties.filter(x => !x.disableWorkflowProviderSelection);

    storageDescriptorOptions = [{value: null, text: 'Default'}, ...storageDescriptorOptions];

    const renderPropertyStorageSelectField = function (propertyDescriptor: ActivityPropertyDescriptor) {
      const propertyName = propertyDescriptor.name;
      const fieldName = `propertyStorageProviders.${propertyName}`;
      return selectField(formContext, fieldName, propertyName, activityModel.propertyStorageProviders[propertyName], storageDescriptorOptions, null, fieldName);
    }

    return (
      <div class="elsa-space-y-8 elsa-w-full">
        {section('Workflow Context')}
        {checkBox(formContext, 'loadWorkflowContext', 'Load Workflow Context', activityModel.loadWorkflowContext, 'When enabled, this will load the workflow context into memory before executing this activity.', 'loadWorkflowContext')}
        {checkBox(formContext, 'saveWorkflowContext', 'Save Workflow Context', activityModel.saveWorkflowContext, 'When enabled, this will save the workflow context back into storage after executing this activity.', 'saveWorkflowContext')}

        {section('Workflow Instance')}
        {checkBox(formContext, 'persistWorkflow', 'Save Workflow Instance', activityModel.persistWorkflow, 'When enabled, this will save the workflow instance back into storage right after executing this activity.', 'persistWorkflow')}

        {Object.keys(outputProperties).length > 0 ? (
          [section('Activity Output', 'Configure the desired storage for each output property of this activity.'), outputProperties.map(renderPropertyStorageSelectField)]
        ) : undefined}

        {Object.keys(inputProperties).length > 0 ? (
          [section('Activity Input', 'Configure the desired storage for each input property of this activity.'), inputProperties.map(renderPropertyStorageSelectField)]
        ) : undefined}
      </div>
    );
  }

  renderCommonTab(activityModel: ActivityModel) {
    const formContext = this.formContext;
    const t = this.t;

    return (
      <div class="elsa-space-y-8 elsa-w-full">
        {textInput(formContext, 'name', t('Tabs.Common.Fields.Name.Label'), activityModel.name, t('Tabs.Common.Fields.Name.Hint'), 'activityName')}
        {textInput(formContext, 'displayName', t('Tabs.Common.Fields.DisplayName.Label'), activityModel.displayName, t('Tabs.Common.Fields.DisplayName.Hint'), 'activityDisplayName')}
        {textArea(formContext, 'description', t('Tabs.Common.Fields.Description.Label'), activityModel.description, t('Tabs.Common.Fields.Description.Hint'), 'activityDescription')}
      </div>
    );
  }

  renderPropertiesTab(activityModel: ActivityModel) {
    const propertyDescriptors: Array<ActivityPropertyDescriptor> = this.renderProps.defaultProperties;

    if (propertyDescriptors.length == 0)
      return undefined;

    const key = `activity-settings:${activityModel.activityId}`;
    const t = this.t;

    return (
      <div key={key} class={`elsa-grid elsa-grid-cols-1 elsa-gap-y-6 elsa-gap-x-4 sm:elsa-grid-cols-6`}>
        {propertyDescriptors.map(property => this.renderPropertyEditor(activityModel, property))}
      </div>
    );
  }

  renderCategoryTab(activityModel: ActivityModel, activityDescriptor: ActivityDescriptor, category: string) {
    const propertyDescriptors: Array<ActivityPropertyDescriptor> = activityDescriptor.inputProperties;
    const descriptors = propertyDescriptors.filter(x => x.category == category);
    const key = `activity-settings:${activityModel.activityId}:${category}`;

    return <div key={key} class={`elsa-grid elsa-grid-cols-1 elsa-gap-y-6 elsa-gap-x-4 sm:elsa-grid-cols-6`}>
      {descriptors.map(property => this.renderPropertyEditor(activityModel, property))}
    </div>;
  }

  renderPropertyEditor(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const key = `activity-property-input:${activity.activityId}:${property.name}`;
    const display = propertyDisplayManager.display(activity, property);
    return <elsa-control key={key} class="sm:elsa-col-span-6" content={display}/>;
  }

  getHiddenClass(tab: string) {
    return this.renderProps.selectedTabName == tab ? '' : 'hidden';
  }
}
