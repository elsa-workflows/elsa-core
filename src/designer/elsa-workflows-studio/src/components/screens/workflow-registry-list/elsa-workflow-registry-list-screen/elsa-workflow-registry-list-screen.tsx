import {Component, h, Prop, State, Event, EventEmitter} from '@stencil/core';
import * as collection from 'lodash/collection';
import {createElsaClient} from "../../../../services/elsa-client";
import {EventTypes, PagedList, VersionOptions, WorkflowBlueprintSummary, ConfigureFeatureContext} from "../../../../models";
import {MenuItem} from "../../../../components/controls/elsa-context-menu/models";
import {FeatureMenuItem} from '../../../../models';
import {RouterHistory} from "@stencil/router";
import {eventBus} from '../../../../services';
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-workflow-registry-list-screen',
  shadow: false,
})
export class ElsaWorkflowRegistryListScreen {
  @Prop() history?: RouterHistory;
  @Prop() serverUrl: string;
  @Prop() basePath: string;
  @Prop() culture: string;
  @Prop({attribute: 'features', reflect: true}) featuresString : string;
  @State() workflowBlueprints: PagedList<WorkflowBlueprintSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};

  confirmDialog: HTMLElsaConfirmDialogElement;
  private featureContexts: Array<ConfigureFeatureContext> = [];

  async componentWillLoad() {
    await this.loadWorkflowBlueprints();
    await this.configureFeatures();
  }

  connectedCallback() {
    eventBus.on(EventTypes.WorkflowUpdated, this.onLoadWorkflowBlueprints);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WorkflowUpdated, this.onLoadWorkflowBlueprints);
  }  

  async configureFeatures() {
    
    const parsedFeatures: string[] = this.featuresString.split(',');

    for (const featureName of parsedFeatures)
    {
      const featureContext: ConfigureFeatureContext = {
        featureName: featureName,
        basePath: this.basePath,
        menuItems: [],
        routes: [],
        headers: [],
        columns: [],
        hasContextItems: false,
        variables: null
      }

      eventBus.emit(EventTypes.ConfigureFeature, this, featureContext);

      featureContext.headers = [...featureContext.headers];
      featureContext.columns = [...featureContext.columns];
      featureContext.hasContextItems = featureContext.hasContextItems;
      this.featureContexts.push(featureContext);
    }
  }

  async onDisableWorkflowClick(e: Event, workflowBlueprintId: string) {
    const result = await this.confirmDialog.show('Disable Workflow', 'Are you sure you wish to disable this workflow?');

    if (!result)
      return;

    await this.updateFeature(workflowBlueprintId, 'disabled', 'true');
  }  

  async onEnableWorkflowClick(e: Event, workflowBlueprintId: string) {
    const result = await this.confirmDialog.show('Enable Workflow', 'Are you sure you wish to enable this workflow?');

    if (!result)
      return;

    await this.updateFeature(workflowBlueprintId, 'disabled', 'false');
  }

  async updateFeature(workflowBlueprintId: string, key: string, value: string)
  {
    let data = JSON.stringify(["settings", workflowBlueprintId, key, value]);
    eventBus.emit(EventTypes.FeatureUpdated, this, data);
  }  

  async onLoadWorkflowBlueprints()
  {
    await this.loadWorkflowBlueprints();
  }

  async loadWorkflowBlueprints() {
    const elsaClient = this.createClient();
    const page = 0;
    const pageSize = 50;
    const versionOptions: VersionOptions = {allVersions: true};
    this.workflowBlueprints = await elsaClient.workflowRegistryApi.list(page, pageSize, versionOptions);
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  render() {
    const workflowBlueprints = this.workflowBlueprints.items;
    const groupings = collection.groupBy(workflowBlueprints, 'id');
    const basePath = this.basePath;
    
    let feature = this.featureContexts.find(x => x.featureName == 'settings');
    let featureEnabled = !!feature;
    let hasFeatureContextItems = featureEnabled ? feature.hasContextItems : false;
    let featureHeaders = featureEnabled ? feature.headers : [];
    let featureColumns = featureEnabled ? feature.columns : [];

    const renderFeatureHeader = (item: FeatureMenuItem) => {
      debugger
      return (<th class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-uppercase elsa-tracking-wider">{item.label}</th>)
    }

    const renderFeatureColumn = (isWorkflowBlueprintDisabled: boolean) => {
      return (<td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-right">{isWorkflowBlueprintDisabled ? 'No' : 'Yes'}</td>)
    }    

    const renderContextMenu = (
      workflowBlueprintId: string, 
      isWorkflowBlueprintDisabled: boolean, 
      history: RouterHistory, 
      editUrl: string, 
      editIcon: any,        
      enableIcon: any, 
      disableIcon: any) => {

      let menuItems: MenuItem[] = [];
      menuItems = [...menuItems, ...[{text: 'Edit', anchorUrl: editUrl, icon: editIcon}]];
      
      if (hasFeatureContextItems)
      {
        if (isWorkflowBlueprintDisabled)
          menuItems = [...menuItems, ...[{text: 'Enable', clickHandler: e => this.onEnableWorkflowClick(e, workflowBlueprintId), icon: enableIcon}]];
        else
          menuItems = [...menuItems, ...[{text: 'Disable', clickHandler: e => this.onDisableWorkflowClick(e, workflowBlueprintId), icon: disableIcon}]];
      }
      return (<td class="elsa-pr-6">
          <elsa-context-menu history={history} menuItems={menuItems}/>
        </td>)      
    }

    return (
      <div>
        <div class="elsa-align-middle elsa-inline-block elsa-min-w-full elsa-border-b elsa-border-gray-200">
          <table class="elsa-min-w-full">
            <thead>
            <tr class="elsa-border-t elsa-border-gray-200">
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-text-left elsa-uppercase elsa-tracking-wider"><span
                class="lg:elsa-pl-2">Name</span></th>
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-text-left elsa-uppercase elsa-tracking-wider">
                Instances
              </th>
              <th class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-uppercase elsa-tracking-wider">
                Latest Version
              </th>
              <th class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-uppercase elsa-tracking-wider">
                Published Version
              </th>
              {featureHeaders.map(item => renderFeatureHeader(item))}
              <th class="elsa-pr-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-text-left elsa-uppercase elsa-tracking-wider"/>
            </tr>
            </thead>
            <tbody class="elsa-bg-white elsa-divide-y elsa-divide-gray-100">
            {collection.map(groupings, group => {
              const versions = collection.orderBy(group, 'version', 'desc');
              const workflowBlueprint: WorkflowBlueprintSummary = versions[0];
              const latestVersionNumber = workflowBlueprint.version;
              const publishedVersion: WorkflowBlueprintSummary = versions.find(x => x.isPublished);
              const publishedVersionNumber = !!publishedVersion ? publishedVersion.version : '-';
              let workflowDisplayName = workflowBlueprint.displayName;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = workflowBlueprint.name;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = 'Untitled';

              const editUrl = `${basePath}/workflow-registry/${workflowBlueprint.id}`;
              const instancesUrl = `${basePath}/workflow-instances?workflow=${workflowBlueprint.id}`;

              const editIcon = (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"
                     stroke-linejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
              );  
          
              const enableIcon = (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                  <path stroke="none" d="M0 0h24v24H0z"/>  
                  <path d="M5 12l5 5l10 -10" />
                </svg>
              );                
          
              const disableIcon = (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                  <path stroke="none" d="M0 0h24v24H0z"/>
                  <circle cx="12" cy="12" r="9" />
                  <line x1="5.7" y1="5.7" x2="18.3" y2="18.3" />
                </svg>
              );               

              return (
                <tr>
                  <td class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                    <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                      <stencil-route-link url={editUrl} anchorClass="elsa-truncate hover:elsa-text-gray-600"><span>{workflowDisplayName}</span></stencil-route-link>
                    </div>
                  </td>

                  <td class="elsa-px-6 elsa-py-3 elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-font-medium">
                    <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                      <stencil-route-link url={instancesUrl} anchorClass="elsa-truncate hover:elsa-text-gray-600">Instances</stencil-route-link>
                    </div>
                  </td>

                  <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-right">{latestVersionNumber}</td>
                  <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-right">{publishedVersionNumber}</td>
                  {featureColumns.map(item => renderFeatureColumn(workflowBlueprint.isDisabled))}
                  {renderContextMenu(workflowBlueprint.id, workflowBlueprint.isDisabled, this.history, editUrl, editIcon, enableIcon, disableIcon)}
                </tr>
              );
            })}
            </tbody>
          </table>
        </div>

        <elsa-confirm-dialog ref={el => this.confirmDialog = el}/>
      </div>
    );
  }
}
Tunnel.injectProps(ElsaWorkflowRegistryListScreen, ['serverUrl', 'culture', 'basePath', 'featuresString']);