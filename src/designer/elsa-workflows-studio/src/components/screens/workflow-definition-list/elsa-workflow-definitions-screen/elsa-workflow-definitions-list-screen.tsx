import {Component, Event, h, Method, Prop, State} from '@stencil/core';
import {createElsaClient} from "../../../../services";
import {PagedList, VersionOptions, WorkflowDefinitionSummary} from "../../../../models";
import {injectHistory, LocationSegments, RouterHistory} from "@stencil/router";
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {GetIntlMessage} from "../../../i18n/intl-message";
import Tunnel from "../../../../data/dashboard";
import {PagerData} from "../../../controls/elsa-pager/elsa-pager";
import {parseQuery} from "../../../../utils/utils";

@Component({
  tag: 'elsa-workflow-definitions-list-screen',
  shadow: false,
})
export class ElsaWorkflowDefinitionsListScreen {
  static readonly DEFAULT_PAGE_SIZE = 15;
  static readonly MIN_PAGE_SIZE = 5;
  static readonly MAX_PAGE_SIZE = 100;
  static readonly START_PAGE = 0;

  @Prop() history?: RouterHistory;
  @Prop({attribute: 'server-url'}) serverUrl: string;
  @Prop() culture: string;
  @Prop() basePath: string;
  @State() workflowDefinitions: PagedList<WorkflowDefinitionSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};
  @State() currentPage: number = 0;
  @State() currentPageSize: number = ElsaWorkflowDefinitionsListScreen.DEFAULT_PAGE_SIZE;
  @State() currentSearchTerm?: string;

  private i18next: i18n;
  private confirmDialog: HTMLElsaConfirmDialogElement;
  private clearRouteChangedListeners: () => void;

  connectedCallback() {
    if (!!this.history)
      this.clearRouteChangedListeners = this.history.listen(e => this.routeChanged(e));
  }

  disconnectedCallback() {
    if (!!this.clearRouteChangedListeners)
      this.clearRouteChangedListeners();
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);

    if (!!this.history)
      this.applyQueryString(this.history.location.search);

    await this.loadWorkflowDefinitions();
  }

  t = (key: string, options?: any) => this.i18next.t(key, options);

  async onSearch(e: Event) {
    e.preventDefault();
    const form = e.currentTarget as HTMLFormElement;
    const formData = new FormData(form);
    const searchTerm: FormDataEntryValue = formData.get('searchTerm');

    this.currentSearchTerm = searchTerm.toString();
    await this.loadWorkflowDefinitions();
  }

  applyQueryString(queryString?: string) {
    const query = parseQuery(queryString);

    this.currentPage = !!query.page ? parseInt(query.page) : 0;
    this.currentPage = isNaN(this.currentPage) ? ElsaWorkflowDefinitionsListScreen.START_PAGE : this.currentPage;
    this.currentPageSize = !!query.pageSize ? parseInt(query.pageSize) : ElsaWorkflowDefinitionsListScreen.DEFAULT_PAGE_SIZE;
    this.currentPageSize = isNaN(this.currentPageSize) ? ElsaWorkflowDefinitionsListScreen.DEFAULT_PAGE_SIZE : this.currentPageSize;
    this.currentPageSize = Math.max(Math.min(this.currentPageSize, ElsaWorkflowDefinitionsListScreen.MAX_PAGE_SIZE), ElsaWorkflowDefinitionsListScreen.MIN_PAGE_SIZE);
  }

  async onPublishClick(e: Event, workflowDefinition: WorkflowDefinitionSummary) {
    const elsaClient = await this.createClient();
    await elsaClient.workflowDefinitionsApi.publish(workflowDefinition.definitionId);
    await this.loadWorkflowDefinitions();
  }

  async onUnPublishClick(e: Event, workflowDefinition: WorkflowDefinitionSummary) {
    const elsaClient = await this.createClient();
    await elsaClient.workflowDefinitionsApi.retract(workflowDefinition.definitionId);
    await this.loadWorkflowDefinitions();
  }

  async onDeleteClick(e: Event, workflowDefinition: WorkflowDefinitionSummary) {
    const t = x => this.i18next.t(x);
    const result = await this.confirmDialog.show(t('DeleteConfirmationModel.Title'), t('DeleteConfirmationModel.Message'));

    if (!result)
      return;

    const elsaClient = await this.createClient();
    await elsaClient.workflowDefinitionsApi.delete(workflowDefinition.definitionId, {allVersions: true});
    await this.loadWorkflowDefinitions();
  }

  async routeChanged(e: LocationSegments) {

    if (!e.pathname.toLowerCase().endsWith('workflow-definitions'))
      return;

    this.applyQueryString(e.search);
    await this.loadWorkflowDefinitions();
  }

  onPaged = async (e: CustomEvent<PagerData>) => {
    this.currentPage = e.detail.page;
    await this.loadWorkflowDefinitions();
  };

  @Method()
  async loadWorkflowDefinitions() {
    const elsaClient = await this.createClient();
    const page = this.currentPage;
    const pageSize = this.currentPageSize;
    const latestOrPublishedVersionOptions: VersionOptions = {isLatestOrPublished: true};
    this.workflowDefinitions = await elsaClient.workflowDefinitionsApi.list(page, pageSize, latestOrPublishedVersionOptions, this.currentSearchTerm);
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  render() {
    const allDefinitions = this.workflowDefinitions.items;
    const latestDefinitions = allDefinitions.filter(x => x.isLatest);
    const publishedDefinitions = allDefinitions.filter(x => x.isPublished);
    const totalCount = this.workflowDefinitions.totalCount;
    const i18next = this.i18next;
    const IntlMessage = GetIntlMessage(i18next);
    const basePath = this.basePath;
    const t = this.t;

    return (
      <div>
        <div
          class="elsa-relative elsa-z-10 elsa-flex-shrink-0 elsa-flex elsa-h-16 elsa-bg-white elsa-border-b elsa-border-gray-200">
          <div class="elsa-flex-1 elsa-px-4 elsa-flex elsa-justify-between sm:elsa-px-6 lg:elsa-px-8">
            <div class="elsa-flex-1 elsa-flex">
              <form class="elsa-w-full elsa-flex md:ml-0" onSubmit={e => this.onSearch(e)}>
                <label htmlFor="search_field" class="elsa-sr-only">Search</label>
                <div class="elsa-relative elsa-w-full elsa-text-gray-400 focus-within:elsa-text-gray-600">
                  <div
                    class="elsa-absolute elsa-inset-y-0 elsa-left-0 elsa-flex elsa-items-center elsa-pointer-events-none">
                    <svg class="elsa-h-5 elsa-w-5" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" clip-rule="evenodd"
                            d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"/>
                    </svg>
                  </div>
                  <input name="searchTerm"
                         class="elsa-block elsa-w-full elsa-h-full elsa-pl-8 elsa-pr-3 elsa-py-2 elsa-rounded-md elsa-text-gray-900 elsa-placeholder-gray-500 focus:elsa-placeholder-gray-400 sm:elsa-text-sm elsa-border-0 focus:elsa-outline-none focus:elsa-ring-0"
                         placeholder={t('Search')}
                         type="search"/>
                </div>
              </form>
            </div>
          </div>
        </div>

        <div class="elsa-align-middle elsa-inline-block elsa-min-w-full elsa-border-b elsa-border-gray-200">
          <table class="elsa-min-w-full">
            <thead>
            <tr class="elsa-border-t elsa-border-gray-200">
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                <span class="lg:elsa-pl-2"><IntlMessage label="Name"/></span>
              </th>
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                <IntlMessage label="Instances"/>
              </th>
              <th class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-right elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                <IntlMessage label="LatestVersion"/>
              </th>
              <th class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-right elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                <IntlMessage label="PublishedVersion"/>
              </th>
              <th class="elsa-pr-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-right elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider"/>
            </tr>
            </thead>
            <tbody class="elsa-bg-white elsa-divide-y elsa-divide-gray-100">
            {latestDefinitions.map(workflowDefinition => {
              const latestVersionNumber = workflowDefinition.version;
              const {isPublished} = workflowDefinition;
              const publishedVersion: WorkflowDefinitionSummary = isPublished ? workflowDefinition : publishedDefinitions.find(x => x.definitionId == workflowDefinition.definitionId);
              const publishedVersionNumber = !!publishedVersion ? publishedVersion.version : '-';
              let workflowDisplayName = workflowDefinition.displayName;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = workflowDefinition.name;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = 'Untitled';

              const editUrl = `${basePath}/workflow-definitions/${workflowDefinition.definitionId}`;
              const instancesUrl = `${basePath}/workflow-instances?workflow=${workflowDefinition.definitionId}`;

              const editIcon = (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"
                     stroke-linejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
              );

              const deleteIcon = (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                  <path stroke="none" d="M0 0h24v24H0z"/>
                  <line x1="4" y1="7" x2="20" y2="7"/>
                  <line x1="10" y1="11" x2="10" y2="17"/>
                  <line x1="14" y1="11" x2="14" y2="17"/>
                  <path d="M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12"/>
                  <path d="M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3"/>
                </svg>
              );

              const publishIcon = (
                <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-5 elsa-w-5 elsa-text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                </svg>
              );

              const unPublishIcon = (
                <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-5 elsa-w-5 elsa-text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"/>
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
                      <stencil-route-link url={instancesUrl} anchorClass="elsa-truncate hover:elsa-text-gray-600"><IntlMessage label="Instances"/></stencil-route-link>
                    </div>
                  </td>

                  <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-right">{latestVersionNumber}</td>
                  <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-right">{publishedVersionNumber}</td>
                  <td class="elsa-pr-6">
                    <elsa-context-menu history={this.history} menuItems={[
                      {text: i18next.t('Edit'), anchorUrl: editUrl, icon: editIcon},
                      isPublished ? {text: i18next.t('Unpublish'), clickHandler: e => this.onUnPublishClick(e, workflowDefinition), icon: unPublishIcon} : {
                        text: i18next.t('Publish'),
                        clickHandler: e => this.onPublishClick(e, workflowDefinition),
                        icon: publishIcon
                      },
                      {text: i18next.t('Delete'), clickHandler: e => this.onDeleteClick(e, workflowDefinition), icon: deleteIcon}
                    ]}/>
                  </td>
                </tr>
              );
            })}
            </tbody>
          </table>
          <elsa-pager page={this.currentPage} pageSize={this.currentPageSize} totalCount={totalCount} history={this.history} onPaged={this.onPaged} culture={this.culture}/>
        </div>

        <elsa-confirm-dialog ref={el => this.confirmDialog = el} culture={this.culture}/>
      </div>
    );
  }
}

Tunnel.injectProps(ElsaWorkflowDefinitionsListScreen, ['serverUrl', 'culture', 'basePath']);
injectHistory(ElsaWorkflowDefinitionsListScreen);
