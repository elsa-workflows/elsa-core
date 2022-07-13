import {Component, h, Method, Prop, State, Watch} from '@stencil/core';
import {injectHistory, LocationSegments, RouterHistory} from "@stencil/router";
import * as collection from 'lodash/collection';
import * as array from 'lodash/array';
import {confirmDialogService, createElsaClient, eventBus} from "../../../../services";
import {EventTypes, OrderBy, PagedList, WorkflowBlueprintSummary, WorkflowInstanceSummary, WorkflowStatus} from "../../../../models";
import {DropdownButtonItem, DropdownButtonOrigin} from "../../../controls/elsa-dropdown-button/models";
import {Map, parseQuery} from '../../../../utils/utils';
import moment from "moment";
import {PagerData} from "../../../controls/elsa-pager/elsa-pager";
import {i18n} from "i18next";
import {resources} from "./localizations";
import {loadTranslations} from "../../../i18n/i18n-loader";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-workflow-instance-list-screen',
  shadow: false,
})
export class ElsaWorkflowInstanceListScreen {
  static readonly DEFAULT_PAGE_SIZE = 15;
  static readonly MIN_PAGE_SIZE = 5;
  static readonly MAX_PAGE_SIZE = 100;
  static readonly START_PAGE = 0;

  @Prop() history?: RouterHistory;
  @Prop() serverUrl: string;
  @Prop() basePath: string;
  @Prop({attribute: 'workflow-id'}) workflowId?: string;
  @Prop({attribute: 'correlation-id'}) correlationId?: string;
  @Prop({attribute: 'workflow-status'}) workflowStatus?: WorkflowStatus;
  @Prop({attribute: 'order-by'}) orderBy?: OrderBy = OrderBy.Started;
  @Prop() culture: string;
  @State() bulkActions: Array<DropdownButtonItem>;
  @State() workflowBlueprints: Array<WorkflowBlueprintSummary> = [];
  @State() workflowInstances: PagedList<WorkflowInstanceSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};
  @State() selectedWorkflowId?: string;
  @State() selectedCorrelationId?: string;
  @State() selectedWorkflowStatus?: WorkflowStatus;
  @State() selectedOrderByState?: OrderBy = OrderBy.Started;
  @State() selectedWorkflowInstanceIds: Array<string> = [];
  @State() selectAllChecked: boolean;

  @State() currentPage: number = 0;
  @State() currentPageSize: number = ElsaWorkflowInstanceListScreen.DEFAULT_PAGE_SIZE;
  @State() currentSearchTerm?: string;

  i18next: i18n;
  selectAllCheckboxEl: any;
  unlistenRouteChanged: () => void;

  connectedCallback() {
    if (!!this.history)
      this.unlistenRouteChanged = this.history.listen(e => this.routeChanged(e));
  }

  disconnectedCallback() {
    if (!!this.unlistenRouteChanged)
      this.unlistenRouteChanged();
  }

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);

    this.selectedWorkflowId = this.workflowId;
    this.selectedCorrelationId = this.correlationId;
    this.selectedWorkflowStatus = this.workflowStatus;
    this.selectedOrderByState = this.orderBy;

    if (!!this.history)
      this.applyQueryString(this.history.location.search);

    await this.loadWorkflowBlueprints();
    await this.loadWorkflowInstances();

    const t = this.t;

    let bulkActions = [{
      text: t('BulkActions.Actions.Cancel'),
      name: 'Cancel',
    }, {
      text: t('BulkActions.Actions.Delete'),
      name: 'Delete',
    }, {
      text: t('BulkActions.Actions.Retry'),
      name: 'Retry',
    }];

    await eventBus.emit(EventTypes.WorkflowInstanceBulkActionsLoading, this, {sender: this, bulkActions});

    this.bulkActions = bulkActions;
  }

  @Method()
  async getSelectedWorkflowInstanceIds() {
    return this.selectedWorkflowInstanceIds;
  }

  @Method()
  async refresh() {
    await this.loadWorkflowInstances();
    this.updateSelectAllChecked();
  }

  @Watch("workflowId")
  async handleWorkflowIdChanged(value: string) {
    this.selectedWorkflowId = value;
    await this.loadWorkflowInstances();
  }

  @Watch("correlationId")
  async handleCorrelationIdChanged(value: string) {
    this.selectedCorrelationId = value;
    await this.loadWorkflowInstances();
  }

  @Watch("workflowStatus")
  async handleWorkflowStatusChanged(value: WorkflowStatus) {
    this.selectedWorkflowStatus = value;
    await this.loadWorkflowInstances();
  }

  @Watch("currentPageSize")
  async handlePageSizeChanged(value: number) {
    this.currentPageSize = value;
    this.currentPageSize = isNaN(this.currentPageSize) ? ElsaWorkflowInstanceListScreen.DEFAULT_PAGE_SIZE : this.currentPageSize;
    this.currentPageSize = Math.max(Math.min(this.currentPageSize, ElsaWorkflowInstanceListScreen.MAX_PAGE_SIZE), ElsaWorkflowInstanceListScreen.MIN_PAGE_SIZE);
    await this.loadWorkflowInstances();
  }

  @Watch("orderBy")
  async handleOrderByChanged(value: OrderBy) {
    this.selectedOrderByState = value;
    await this.loadWorkflowInstances();
  }

  t = (key: string, options?: any) => this.i18next.t(key, options);

  private applyQueryString(queryString?: string) {
    const query = parseQuery(queryString);

    this.selectedWorkflowId = query.workflow;
    this.correlationId = query.correlationId;
    this.selectedWorkflowStatus = query.status;
    this.selectedOrderByState = query.orderBy ?? OrderBy.Started;
    this.currentPage = !!query.page ? parseInt(query.page) : 0;
    this.currentPage = isNaN(this.currentPage) ? ElsaWorkflowInstanceListScreen.START_PAGE : this.currentPage;
    this.currentPageSize = !!query.pageSize ? parseInt(query.pageSize) : ElsaWorkflowInstanceListScreen.DEFAULT_PAGE_SIZE;
    this.currentPageSize = isNaN(this.currentPageSize) ? ElsaWorkflowInstanceListScreen.DEFAULT_PAGE_SIZE : this.currentPageSize;
    this.currentPageSize = Math.max(Math.min(this.currentPageSize, ElsaWorkflowInstanceListScreen.MAX_PAGE_SIZE), ElsaWorkflowInstanceListScreen.MIN_PAGE_SIZE);
  }

  private async loadWorkflowBlueprints() {
    const elsaClient = await this.createClient();
    this.workflowBlueprints = await elsaClient.workflowRegistryApi.listAll({allVersions: true});
  }

  private async loadWorkflowInstances() {
    this.currentPage = isNaN(this.currentPage) ? ElsaWorkflowInstanceListScreen.START_PAGE : this.currentPage;
    this.currentPage = Math.max(this.currentPage, ElsaWorkflowInstanceListScreen.START_PAGE);
    this.currentPageSize = isNaN(this.currentPageSize) ? ElsaWorkflowInstanceListScreen.DEFAULT_PAGE_SIZE : this.currentPageSize;
    const elsaClient = await this.createClient();
    this.workflowInstances = await elsaClient.workflowInstancesApi.list(this.currentPage, this.currentPageSize, this.selectedWorkflowId, this.selectedWorkflowStatus, this.selectedOrderByState, this.currentSearchTerm, this.correlationId);
    const maxPage = Math.floor(this.workflowInstances.totalCount / this.currentPageSize);

    if (this.currentPage > maxPage) {
      this.currentPage = maxPage;
      this.workflowInstances = await elsaClient.workflowInstancesApi.list(this.currentPage, this.currentPageSize, this.selectedWorkflowId, this.selectedWorkflowStatus, this.selectedOrderByState, this.currentSearchTerm, this.correlationId);
    }

    this.setSelectAllIndeterminateState();
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  getLatestWorkflowBlueprintVersions(): Array<WorkflowBlueprintSummary> {
    const groups = collection.groupBy(this.workflowBlueprints, 'id');
    return collection.map(groups, x => array.first(collection.orderBy(x, 'version', 'desc')));
  }

  buildFilterUrl(workflowId?: string, workflowStatus?: WorkflowStatus, orderBy?: OrderBy, pageSize?: number, correlationId?: string) {
    const filters: Map<string> = {};

    if (!!correlationId)
      filters['correlationId'] = correlationId;

    if (!!workflowId)
      filters['workflow'] = workflowId;

    if (!!workflowStatus)
      filters['status'] = workflowStatus;

    if (!!orderBy)
      filters['orderBy'] = orderBy;

    if (!!this.currentPage)
      filters['page'] = this.currentPage.toString();

    let newPageSize = !!pageSize ? pageSize : this.currentPageSize;
    newPageSize = Math.max(Math.min(newPageSize, 100), ElsaWorkflowInstanceListScreen.MIN_PAGE_SIZE);
    filters['pageSize'] = newPageSize.toString();

    if (newPageSize != this.currentPageSize)
      filters['page'] = Math.floor(this.currentPage * this.currentPageSize / newPageSize).toString();

    const queryString = collection.map(filters, (v, k) => `${k}=${v}`).join('&');
    return `${this.basePath}/workflow-instances?${queryString}`;
  }

  getStatusColor(status: WorkflowStatus) {
    switch (status) {
      default:
      case WorkflowStatus.Idle:
        return "gray";
      case WorkflowStatus.Running:
        return "rose";
      case WorkflowStatus.Suspended:
        return "blue";
      case WorkflowStatus.Finished:
        return "green";
      case WorkflowStatus.Faulted:
        return "red";
      case WorkflowStatus.Cancelled:
        return "yellow";
    }
  }

  updateSelectAllChecked() {
    if (this.workflowInstances.items.length == 0) {
      this.selectAllChecked = false;
      return;
    }

    this.selectAllChecked = this.workflowInstances.items.findIndex(x => this.selectedWorkflowInstanceIds.findIndex(id => id == x.id) < 0) < 0;
  }

  async routeChanged(e: LocationSegments) {

    if (!e.pathname.toLowerCase().endsWith('workflow-instances'))
      return;

    this.applyQueryString(e.search);
    await this.loadWorkflowInstances();
  }

  onSelectAllCheckChange(e: Event) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;

    this.selectAllChecked = isChecked;

    if (isChecked) {
      let itemsToAdd = [];
      this.workflowInstances.items.forEach(item => {
        if (!this.selectedWorkflowInstanceIds.includes(item.id)) {
          itemsToAdd.push(item.id);
        }
      });

      if (itemsToAdd.length > 0) {
        this.selectedWorkflowInstanceIds = this.selectedWorkflowInstanceIds.concat(itemsToAdd);
      }
    } else {
      const currentItems = this.workflowInstances.items.map(x => x.id);
      this.selectedWorkflowInstanceIds = this.selectedWorkflowInstanceIds.filter(item => {
        return !currentItems.includes(item);
      });
    }
  }

  getSelectAllState = () => {
    const {items} = this.workflowInstances;

    for (let i = 0; i < items.length; i++) {
      if (!this.selectedWorkflowInstanceIds.includes(items[i].id)) {
        return false;
      }
    }

    return true;
  }

  setSelectAllIndeterminateState = () => {
    if (this.selectAllCheckboxEl) {
      const selectedItems = this.workflowInstances.items.filter(item => this.selectedWorkflowInstanceIds.includes(item.id));
      this.selectAllCheckboxEl.indeterminate = selectedItems.length != 0 && selectedItems.length != this.workflowInstances.items.length;
    }
  }

  onWorkflowInstanceCheckChange(e: Event, workflowInstance: WorkflowInstanceSummary) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;

    if (isChecked)
      this.selectedWorkflowInstanceIds = [...this.selectedWorkflowInstanceIds, workflowInstance.id];
    else
      this.selectedWorkflowInstanceIds = this.selectedWorkflowInstanceIds.filter(x => x != workflowInstance.id);

    this.setSelectAllIndeterminateState();
  }

  async onCancelClick(e: Event, workflowInstance: WorkflowInstanceSummary) {
    const t = this.t;
    const result = await confirmDialogService.show(t('CancelDialog.Title'), t('CancelDialog.Message'));

    if (!result)
      return;

    const elsaClient = await this.createClient();
    await elsaClient.workflowInstancesApi.cancel(workflowInstance.id);
    await this.loadWorkflowInstances();
  }

  async onDeleteClick(e: Event, workflowInstance: WorkflowInstanceSummary) {
    const t = this.t;
    const result = await confirmDialogService.show(t('DeleteDialog.Title'), t('DeleteDialog.Message'));

    if (!result)
      return;

    const elsaClient = await this.createClient();
    await elsaClient.workflowInstancesApi.delete(workflowInstance.id);
    await this.loadWorkflowInstances();
  }

  async onRetryClick(e: Event, workflowInstance: WorkflowInstanceSummary) {
    const t = this.t;
    const result = await confirmDialogService.show(t('RetryDialog.Title'), t('RetryDialog.Message'));

    if (!result)
      return;

    const elsaClient = await this.createClient();
    await elsaClient.workflowInstancesApi.retry(workflowInstance.id);
    await this.loadWorkflowInstances();
  }

  async onBulkCancel() {
    const t = this.t;
    const result = await confirmDialogService.show(t('BulkCancelDialog.Title'), t('BulkCancelDialog.Message'));

    if (!result)
      return;

    const elsaClient = await this.createClient();
    await elsaClient.workflowInstancesApi.bulkCancel({workflowInstanceIds: this.selectedWorkflowInstanceIds});
    this.selectedWorkflowInstanceIds = [];
    await this.loadWorkflowInstances();
    this.currentPage = 0;
  }

  async onBulkDelete() {
    const t = this.t;
    const result = await confirmDialogService.show(t('BulkDeleteDialog.Title'), t('BulkDeleteDialog.Message'));

    if (!result)
      return;

    const elsaClient = await this.createClient();
    await elsaClient.workflowInstancesApi.bulkDelete({workflowInstanceIds: this.selectedWorkflowInstanceIds});
    this.selectedWorkflowInstanceIds = [];
    await this.loadWorkflowInstances();
    this.currentPage = 0;
  }

  async onBulkRetry() {
    const t = this.t;
    const result = await confirmDialogService.show(t('BulkRetryDialog.Title'), t('BulkRetryDialog.Message'));

    if (!result)
      return;

    const elsaClient = await this.createClient();
    await elsaClient.workflowInstancesApi.bulkRetry({workflowInstanceIds: this.selectedWorkflowInstanceIds});
    this.selectedWorkflowInstanceIds = [];
    await this.loadWorkflowInstances();
    this.currentPage = 0;
  }

  async onBulkActionSelected(e: CustomEvent<DropdownButtonItem>) {
    const action = e.detail;

    switch (action.name) {
      case 'Cancel':
        await this.onBulkCancel();
        break;
      case 'Delete':
        await this.onBulkDelete();
        break;
      case 'Retry':
        await this.onBulkRetry();
        break;
      default:
        action.handler();
    }

    this.updateSelectAllChecked();
  }

  async onSearch(e: Event) {
    e.preventDefault();
    const form = e.currentTarget as HTMLFormElement;
    const formData = new FormData(form);
    const searchTerm: FormDataEntryValue = formData.get('searchTerm');

    this.currentSearchTerm = searchTerm.toString();
    await this.loadWorkflowInstances();
  }

  onPaged = async (e: CustomEvent<PagerData>) => {
    this.currentPage = e.detail.page;
    await this.loadWorkflowInstances();
  };

  render() {
    const basePath = this.basePath;
    const workflowInstances = this.workflowInstances.items;
    const workflowBlueprints = this.workflowBlueprints;
    const totalCount = this.workflowInstances.totalCount
    const t = this.t;

    const renderViewIcon = function () {
      return (
        <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24"
             xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2"
             stroke-linecap="round" stroke-linejoin="round">
          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
          <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
        </svg>
      );
    };

    const renderCancelIcon = function () {
      return (
        <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2"
             stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M9 10a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z"/>
        </svg>
      );
    };

    const renderDeleteIcon = function () {
      return (
        <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2"
             stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
          <path stroke="none" d="M0 0h24v24H0z"/>
          <line x1="4" y1="7" x2="20" y2="7"/>
          <line x1="10" y1="11" x2="10" y2="17"/>
          <line x1="14" y1="11" x2="14" y2="17"/>
          <path d="M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12"/>
          <path d="M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3"/>
        </svg>
      );
    };

    const renderRetryIcon = function () {
      return (
        <svg class="elsa-h-5 w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
          <path stroke="none" d="M0 0h24v24H0z"/>
          <path d="M12 17l-2 2l2 2m-2 -2h9a2 2 0 0 0 1.75 -2.75l-.55 -1"/>
          <path d="M12 17l-2 2l2 2m-2 -2h9a2 2 0 0 0 1.75 -2.75l-.55 -1" transform="rotate(120 12 13)"/>
          <path d="M12 17l-2 2l2 2m-2 -2h9a2 2 0 0 0 1.75 -2.75l-.55 -1" transform="rotate(240 12 13)"/>
        </svg>
      );
    };

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

        <div class="elsa-p-8 elsa-flex elsa-content-end elsa-justify-right elsa-bg-white elsa-space-x-4">
          <div class="elsa-flex-shrink-0">
            {this.renderBulkActions()}
          </div>
          <div class="elsa-flex-1">
            &nbsp;
          </div>
          {this.renderPageSizeFilter()}
          {this.renderWorkflowFilter()}
          {this.renderStatusFilter()}
          {this.renderOrderByFilter()}
        </div>

        <div class="elsa-mt-8 sm:elsa-block">
          <div class="elsa-align-middle elsa-inline-block elsa-min-w-full elsa-border-b elsa-border-gray-200">
            <table class="elsa-min-w-full">
              <thead>
              <tr class="elsa-border-t elsa-border-gray-200">
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  <input type="checkbox" value="true" checked={this.getSelectAllState()}
                         onChange={e => this.onSelectAllCheckChange(e)}
                         ref={el => this.selectAllCheckboxEl = el}
                         class="focus:elsa-ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300 elsa-rounded"/>
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.Id')}
                </th>
                <th
                  class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.CorrelationId')}
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.Workflow')}
                </th>
                <th
                  class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-right elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.Version')}
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.InstanceName')}
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.Status')}
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.Created')}
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.Finished')}
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.LastExecuted')}
                </th>
                <th
                  class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  {t('Table.Faulted')}
                </th>
                <th
                  class="elsa-pr-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider"/>
              </tr>
              </thead>
              <tbody class="elsa-bg-white elsa-divide-y elsa-divide-gray-100">
              {workflowInstances.map(workflowInstance => {
                const workflowBlueprint = workflowBlueprints.find(x => x.versionId == workflowInstance.definitionVersionId) ?? {
                  name: 'Not Found',
                  displayName: '(Workflow definition not found)'
                };
                const displayName = workflowBlueprint.displayName || workflowBlueprint.name || '(Untitled)';
                const statusColor = this.getStatusColor(workflowInstance.workflowStatus);
                const instanceViewUrl = `${basePath}/workflow-instances/${workflowInstance.id}`;
                const correlationId = !!workflowInstance.correlationId ? workflowInstance.correlationId : ''
                const correlationListViewUrl = `${basePath}/workflow-instances?correlationId=${correlationId}`;
                const blueprintViewUrl = `${basePath}/workflow-registry/${workflowInstance.definitionId}`;
                const instanceName = !workflowInstance.name ? '' : workflowInstance.name;
                const isSelected = this.selectedWorkflowInstanceIds.findIndex(x => x === workflowInstance.id) >= 0;
                const createdAt = moment(workflowInstance.createdAt);
                const finishedAt = !!workflowInstance.finishedAt ? moment(workflowInstance.finishedAt) : null;
                const lastExecutedAt = !!workflowInstance.lastExecutedAt ? moment(workflowInstance.lastExecutedAt) : null;
                const faultedAt = !!workflowInstance.faultedAt ? moment(workflowInstance.faultedAt) : null;
                const isFaulted = workflowInstance.workflowStatus == WorkflowStatus.Faulted;

                const contextMenuItems = [
                  {text: t('Table.ContextMenu.View'), anchorUrl: instanceViewUrl, icon: renderViewIcon()},
                  {
                    text: t('Table.ContextMenu.Cancel'),
                    clickHandler: e => this.onCancelClick(e, workflowInstance),
                    icon: renderCancelIcon()
                  },
                    ...[isFaulted ? {
                      text: t('Table.ContextMenu.Retry'),
                      clickHandler: e => this.onRetryClick(e, workflowInstance),
                      icon: renderRetryIcon()
                    } : null],
                  {
                    text: t('Table.ContextMenu.Delete'),
                    clickHandler: e => this.onDeleteClick(e, workflowInstance),
                    icon: renderDeleteIcon()
                  }
                ].filter(x => x != null);

                return <tr>
                  <td
                    class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                    <input type="checkbox" value={workflowInstance.id} checked={isSelected}
                           onChange={e => this.onWorkflowInstanceCheckChange(e, workflowInstance)}
                           class="focus:elsa-ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300 elsa-rounded"/>
                  </td>
                  <td
                    class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                    <stencil-route-link url={instanceViewUrl}
                                        anchorClass="elsa-truncate hover:elsa-text-gray-600">{workflowInstance.id}</stencil-route-link>
                  </td>
                  <td
                    class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                    <stencil-route-link url={correlationListViewUrl}
                                        anchorClass="elsa-truncate hover:elsa-text-gray-600">{correlationId}</stencil-route-link>
                  </td>
                  <td
                    class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900 elsa-text-left">
                    <stencil-route-link url={blueprintViewUrl} anchorClass="elsa-truncate hover:elsa-text-gray-600">
                      {displayName}
                    </stencil-route-link>
                  </td>
                  <td
                    class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-right elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-uppercase">
                    {workflowInstance.version}
                  </td>
                  <td
                    class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900 elsa-text-left">
                    <stencil-route-link url={instanceViewUrl}
                                        anchorClass="elsa-truncate hover:elsa-text-gray-600">{instanceName}</stencil-route-link>
                  </td>
                  <td
                    class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-right">
                    <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                      <div class={`flex-shrink-0 elsa-w-2-5 elsa-h-2-5 elsa-rounded-full elsa-bg-${statusColor}-600`}/>
                      <span>{workflowInstance.workflowStatus}</span>
                    </div>
                  </td>
                  <td
                    class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                    {createdAt.format('DD-MM-YYYY HH:mm:ss')}
                  </td>
                  <td
                    class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                    {!!finishedAt ? finishedAt.format('DD-MM-YYYY HH:mm:ss') : '-'}
                  </td>
                  <td
                    class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                    {!!lastExecutedAt ? lastExecutedAt.format('DD-MM-YYYY HH:mm:ss') : '-'}
                  </td>
                  <td
                    class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                    {!!faultedAt ? faultedAt.format('DD-MM-YYYY HH:mm:ss') : '-'}
                  </td>
                  <td class="elsa-pr-6">
                    <elsa-context-menu history={this.history} menuItems={contextMenuItems}/>
                  </td>
                </tr>
              })}
              </tbody>
            </table>
            <elsa-pager page={this.currentPage} pageSize={this.currentPageSize} totalCount={totalCount} history={this.history} onPaged={this.onPaged} culture={this.culture}/>
          </div>
        </div>
      </div>
    );
  }

  renderBulkActions() {
    const bulkActionIcon = <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24"
                                stroke="currentColor">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M13 10V3L4 14h7v7l9-11h-7z"/>
    </svg>;

    const t = this.t;
    const actions = this.bulkActions;

    return <elsa-dropdown-button text={t('BulkActions.Title')} items={actions} icon={bulkActionIcon}
                                 origin={DropdownButtonOrigin.TopLeft}
                                 onItemSelected={e => this.onBulkActionSelected(e)}/>
  }

  renderWorkflowFilter() {
    const t = this.t;
    const latestWorkflowBlueprints = this.getLatestWorkflowBlueprintVersions();
    const selectedCorrelationId = this.selectedCorrelationId;
    const selectedWorkflowId = this.selectedWorkflowId;
    const selectedWorkflow = latestWorkflowBlueprints.find(x => x.id == selectedWorkflowId);
    const selectedWorkflowText = !selectedWorkflowId ? t('Filters.Workflow.Label') : !!selectedWorkflow && (selectedWorkflow.name || selectedWorkflow.displayName) ? (selectedWorkflow.displayName || selectedWorkflow.name) : t('Untitled');
    const selectedWorkflowStatus = this.selectedWorkflowStatus;
    const selectedOrderBy = this.selectedOrderByState;
    const history = this.history;

    let items: Array<DropdownButtonItem> = latestWorkflowBlueprints.map(x => {
      const displayName = !!x.displayName && x.displayName.length > 0 ? x.displayName : x.name || t('Untitled');
      const item: DropdownButtonItem = {text: displayName, value: x.id, isSelected: x.id == selectedWorkflowId};

      if (!!history)
        item.url = this.buildFilterUrl(x.id, selectedWorkflowStatus, selectedOrderBy, null, selectedCorrelationId);

      return item;
    });

    const allItem: DropdownButtonItem = {text: t('Filters.Workflow.All'), value: null, isSelected: !selectedWorkflowId};

    if (!!history)
      allItem.url = this.buildFilterUrl(null, selectedWorkflowStatus, selectedOrderBy, null, selectedCorrelationId);

    items = [allItem, ...items];

    const renderIcon = function () {
      return <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24"
                  stroke="currentColor">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <rect x="4" y="4" width="6" height="6" rx="1"/>
        <rect x="14" y="4" width="6" height="6" rx="1"/>
        <rect x="4" y="14" width="6" height="6" rx="1"/>
        <rect x="14" y="14" width="6" height="6" rx="1"/>
      </svg>;
    };

    return <elsa-dropdown-button text={selectedWorkflowText} items={items} icon={renderIcon()}
                                 origin={DropdownButtonOrigin.TopRight}
                                 onItemSelected={e => this.handleWorkflowIdChanged(e.detail.value)}/>
  }

  renderStatusFilter() {
    const t = this.t;
    const selectedCorrelationId = this.correlationId;
    const selectedWorkflowStatus = this.selectedWorkflowStatus;
    const selectedWorkflowStatusText = !!selectedWorkflowStatus ? selectedWorkflowStatus : t('Filters.Status.Label');
    const statuses: Array<WorkflowStatus> = [null, WorkflowStatus.Running, WorkflowStatus.Suspended, WorkflowStatus.Finished, WorkflowStatus.Faulted, WorkflowStatus.Cancelled, WorkflowStatus.Idle];
    const history = this.history;

    const items: Array<DropdownButtonItem> = statuses.map(x => {
      const text = x ?? t('Filters.Status.All');
      const item: DropdownButtonItem = {text: text, isSelected: x == selectedWorkflowStatus, value: x};

      if (!!history)
        item.url = this.buildFilterUrl(this.selectedWorkflowId, x, this.selectedOrderByState, null, selectedCorrelationId);

      return item;
    });

    const renderIcon = function () {
      return <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24"
                  stroke="currentColor">
        <circle cx="12" cy="12" r="10"/>
        <polygon points="10 8 16 12 10 16 10 8"/>
      </svg>
    };

    return <elsa-dropdown-button text={selectedWorkflowStatusText} items={items} icon={renderIcon()}
                                 origin={DropdownButtonOrigin.TopRight}
                                 onItemSelected={e => this.handleWorkflowStatusChanged(e.detail.value)}/>
  }

  renderPageSizeFilter() {
    const t = this.t;
    const selectedCorrelationId = this.correlationId;
    const currentPageSize = this.currentPageSize;
    const currentPageSizeText = t('Filters.PageSize.SelectedLabel', {Size: currentPageSize});
    const pageSizes: Array<number> = [5, 10, 15, 20, 30, 50, 100];
    const history = this.history;

    const items: Array<DropdownButtonItem> = pageSizes.map(x => {
      const text = "" + x;
      const item: DropdownButtonItem = {text: text, isSelected: x == currentPageSize, value: x};

      if (!!history)
        item.url = this.buildFilterUrl(this.selectedWorkflowId, this.selectedWorkflowStatus, this.selectedOrderByState, x, selectedCorrelationId);

      return item;
    });

    const renderIcon = function () {
      return <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-400 elsa-mr-2" width="24" height="24" viewBox="0 0 24 24"
                  stroke-width="2"
                  stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <line x1="9" y1="6" x2="20" y2="6"/>
        <line x1="9" y1="12" x2="20" y2="12"/>
        <line x1="9" y1="18" x2="20" y2="18"/>
        <line x1="5" y1="6" x2="5" y2="6.01"/>
        <line x1="5" y1="12" x2="5" y2="12.01"/>
        <line x1="5" y1="18" x2="5" y2="18.01"/>
      </svg>
    };

    return <elsa-dropdown-button text={currentPageSizeText} items={items} icon={renderIcon()}
                                 origin={DropdownButtonOrigin.TopRight}
                                 onItemSelected={e => this.handlePageSizeChanged(e.detail.value)}/>
  }

  renderOrderByFilter() {
    const t = this.t;
    const selectedCorrelationId = this.correlationId;
    const selectedOrderBy = this.selectedOrderByState;
    const selectedOrderByText = !!selectedOrderBy ? t('Filters.Sort.SelectedLabel', {Key: selectedOrderBy}) : t('Filters.Sort.Label');
    const orderByValues: Array<OrderBy> = [OrderBy.Finished, OrderBy.LastExecuted, OrderBy.Started];
    const history = this.history;

    const items: Array<DropdownButtonItem> = orderByValues.map(x => {
      const item: DropdownButtonItem = {text: x, value: x, isSelected: x == selectedOrderBy};

      if (!!history)
        item.url = this.buildFilterUrl(this.selectedWorkflowId, this.selectedWorkflowStatus, x, null, selectedCorrelationId);

      return item;
    });

    const renderIcon = function () {
      return <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24"
                  stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M3 4h13M3 8h9m-9 4h9m5-4v12m0 0l-4-4m4 4l4-4"/>
      </svg>
    };

    return <elsa-dropdown-button text={selectedOrderByText} items={items} icon={renderIcon()}
                                 origin={DropdownButtonOrigin.TopRight}
                                 onItemSelected={e => this.handleOrderByChanged(e.detail.value)}/>
  }
}

Tunnel.injectProps(ElsaWorkflowInstanceListScreen, ['serverUrl', 'culture', 'basePath']);
injectHistory(ElsaWorkflowInstanceListScreen);
