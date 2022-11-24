import { Component, Event, h, Host, Prop, State } from '@stencil/core';
import { eventBus, propertyDisplayManager } from '../../../../services';
import state from '../../../../utils/store';
import { ActivityDescriptor, ActivityModel, ActivityPropertyDescriptor, EventTypes, WorkflowStorageDescriptor } from '../../../../models';
import { checkBox, FormContext, section, selectField, SelectOption, textArea, textInput } from '../../../../utils/forms';
import { i18n } from 'i18next';
import { loadTranslations } from '../../../i18n/i18n-loader';
import { resources } from './localizations';

export interface TabModel {
  tabId: string;
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

export interface ActivityEditorEventArgs {
  activityDescriptor: ActivityDescriptor;
  activityModel: ActivityModel;
}

export interface ActivityEditorAppearingEventArgs extends ActivityEditorEventArgs {}

export interface ActivityEditorDisappearingEventArgs extends ActivityEditorEventArgs {}

@Component({
  tag: 'elsa-activity-editor-panel',
  shadow: false,
})
export class ElsaActivityEditorPanel {
  @Prop() culture: string;
  @State() workflowStorageDescriptors: Array<WorkflowStorageDescriptor> = [];
  @State() activityModel: ActivityModel;
  @State() activityDescriptor: ActivityDescriptor;
  @State() renderProps: ActivityEditorRenderProps = {};
  i18next: i18n;
  formContext: FormContext;
  formElement: HTMLFormElement;
  @State() updateCounter = 0;

  // Force a new key every time we show the editor to make sure Stencil creates new components.
  // This prevents the issue where the designer has e.g. one activity where the user edits the properties, cancels out, then opens the editor again, seeing the entered value still there.
  timestamp: Date = new Date();

  connectedCallback() {
    eventBus.on(EventTypes.ActivityEditor.Show, this.onShowActivityEditor);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ActivityEditor.Show, this.onShowActivityEditor);
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  t = (key: string) => this.i18next.t(key);

  updateActivity(formData: FormData) {
    const activity = this.activityModel;
    const activityDescriptor = this.activityDescriptor;
    const inputProperties: Array<ActivityPropertyDescriptor> = activityDescriptor.inputProperties;
    for (const property of inputProperties) propertyDisplayManager.update(activity, property, formData);
  }

  async componentWillRender() {
    const activityDescriptor: ActivityDescriptor = this.activityDescriptor || {
      displayName: '',
      type: '',
      outcomes: [],
      category: '',
      traits: 0,
      browsable: false,
      inputProperties: [],
      outputProperties: [],
      description: '',
      customAttributes: {},
    };

    const propertyCategories = activityDescriptor.inputProperties
      .filter(x => x.category)
      .map(x => x.category)
      .distinct();
    const defaultProperties = activityDescriptor.inputProperties.filter(x => !x.category || x.category.length == 0);

    const activityModel: ActivityModel = this.activityModel || {
      type: '',
      activityId: '',
      outcomes: [],
      properties: [],
      propertyStorageProviders: {},
    };

    const t = this.t;
    let tabs: Array<TabModel> = [];

    if (defaultProperties.length > 0) {
      tabs.push({
        tabId: 'properties',
        tabName: t('Tabs.Properties.Name'),
        renderContent: () => this.renderPropertiesTab(activityModel),
      });
    }

    for (const category of propertyCategories) {
      const categoryTab: TabModel = {
        tabId: 'categories',
        tabName: category,
        renderContent: () => this.renderCategoryTab(activityModel, activityDescriptor, category),
      };

      tabs.push(categoryTab);
    }

    tabs.push({
      tabId: 'common',
      tabName: t('Tabs.Common.Name'),
      renderContent: () => this.renderCommonTab(activityModel),
    });

    tabs.push({
      tabId: 'storage',
      tabName: t('Tabs.Storage.Name'),
      renderContent: () => this.renderStorageTab(activityModel, activityDescriptor),
    });

    this.renderProps = {
      activityDescriptor,
      activityModel,
      propertyCategories,
      defaultProperties,
      tabs,
      selectedTabName: this.renderProps.selectedTabName,
    };

    await eventBus.emit(EventTypes.ActivityEditor.Rendering, this, this.renderProps);

    let selectedTabName = this.renderProps.selectedTabName;
    tabs = this.renderProps.tabs;

    if (!selectedTabName) this.renderProps.selectedTabName = tabs[0].tabName;

    if (tabs.findIndex(x => x.tabName === selectedTabName) < 0) this.renderProps.selectedTabName = selectedTabName = tabs[0].tabName;
  }

  async componentDidRender() {
    await eventBus.emit(EventTypes.ActivityEditor.Rendered, this, this.renderProps);
  }

  onShowActivityEditor = async (activity: ActivityModel, animate: boolean) => {
    this.activityModel = activity;
    this.activityDescriptor = state.activityDescriptors.find(x => x.type == activity.type);
    this.workflowStorageDescriptors = state.workflowStorageDescriptors;
    this.formContext = new FormContext(this.activityModel, newValue => (this.activityModel = newValue));
    this.timestamp = new Date();
    this.renderProps = {};
    this.updateCounter++;
  };

  onChange = async (e?: Event) => {
    const formData = new FormData(this.formElement);
    this.updateActivity(formData);
    await eventBus.emit(EventTypes.UpdateActivity, this, this.activityModel);
  };

  render() {
    const renderProps = this.renderProps;
    const tabs = renderProps.tabs;

    return (
      <form onChange={this.onChange} ref={el => this.formElement = el}>
        <elsa-flyout-panel autoExpand silent updateCounter={this.updateCounter}>
          {tabs.map(tab => [
            <elsa-tab-header tab={tab.tabName} slot="header">
              {tab.tabName}
            </elsa-tab-header>,
            <elsa-tab-content tab={tab.tabName} slot="content">
              <div class="elsa-pt-4 elsa-ml-1">
                <elsa-control content={tab.renderContent()} />
              </div>
            </elsa-tab-content>,
          ])}
        </elsa-flyout-panel>
      </form>
    );
  }

  renderStorageTab(activityModel: ActivityModel, activityDescriptor: ActivityDescriptor) {
    const formContext = this.formContext;
    const t = this.t;
    let storageDescriptorOptions: Array<SelectOption> = this.workflowStorageDescriptors.map(x => ({
      value: x.name,
      text: x.displayName,
    }));
    let outputProperties = activityDescriptor.outputProperties.filter(x => !x.disableWorkflowProviderSelection);
    let inputProperties = activityDescriptor.inputProperties.filter(x => !x.disableWorkflowProviderSelection);

    storageDescriptorOptions = [{ value: null, text: 'Default' }, ...storageDescriptorOptions];

    const renderPropertyStorageSelectField = function (propertyDescriptor: ActivityPropertyDescriptor) {
      const propertyName = propertyDescriptor.name;
      const fieldName = `propertyStorageProviders.${propertyName}`;
      return selectField(formContext, fieldName, propertyName, activityModel.propertyStorageProviders[propertyName], storageDescriptorOptions, null, fieldName);
    };

    return (
      <div class="elsa-space-y-8 elsa-w-full">
        {section('Workflow Context')}
        {checkBox(
          formContext,
          'loadWorkflowContext',
          'Load Workflow Context',
          activityModel.loadWorkflowContext,
          'When enabled, this will load the workflow context into memory before executing this activity.',
          'loadWorkflowContext',
        )}
        {checkBox(
          formContext,
          'saveWorkflowContext',
          'Save Workflow Context',
          activityModel.saveWorkflowContext,
          'When enabled, this will save the workflow context back into storage after executing this activity.',
          'saveWorkflowContext',
        )}

        {section('Workflow Instance')}
        {checkBox(
          formContext,
          'persistWorkflow',
          'Save Workflow Instance',
          activityModel.persistWorkflow,
          'When enabled, this will save the workflow instance back into storage right after executing this activity.',
          'persistWorkflow',
        )}

        {Object.keys(outputProperties).length > 0
          ? [section('Activity Output', 'Configure the desired storage for each output property of this activity.'), outputProperties.map(renderPropertyStorageSelectField)]
          : undefined}

        {Object.keys(inputProperties).length > 0
          ? [section('Activity Input', 'Configure the desired storage for each input property of this activity.'), inputProperties.map(renderPropertyStorageSelectField)]
          : undefined}
      </div>
    );
  }

  renderCommonTab(activityModel: ActivityModel) {
    const formContext = this.formContext;
    const t = this.t;

    return (
      <div class="elsa-space-y-8 elsa-w-full">
        {textInput(formContext, 'name', t('Tabs.Common.Fields.Name.Label'), activityModel.name, t('Tabs.Common.Fields.Name.Hint'), 'activityName')}
        {textInput(
          formContext,
          'displayName',
          t('Tabs.Common.Fields.DisplayName.Label'),
          activityModel.displayName,
          t('Tabs.Common.Fields.DisplayName.Hint'),
          'activityDisplayName',
        )}
        {textArea(
          formContext,
          'description',
          t('Tabs.Common.Fields.Description.Label'),
          activityModel.description,
          t('Tabs.Common.Fields.Description.Hint'),
          'activityDescription',
        )}
      </div>
    );
  }

  renderPropertiesTab(activityModel: ActivityModel) {
    const propertyDescriptors: Array<ActivityPropertyDescriptor> = this.renderProps.defaultProperties;

    if (propertyDescriptors.length == 0) return undefined;

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

    return (
      <div key={key} class={`elsa-grid elsa-grid-cols-1 elsa-gap-y-6 elsa-gap-x-4 sm:elsa-grid-cols-6`}>
        {descriptors.map(property => this.renderPropertyEditor(activityModel, property))}
      </div>
    );
  }

  renderPropertyEditor(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const key = `activity-property-input:${activity.activityId}:${property.name}`;

    const display = propertyDisplayManager.display(activity, property, this.onChange);
    const id = `${property.name}Control`;
    return <elsa-control key={key} id={id} class="sm:elsa-col-span-6" content={display}/>;
  }
}
