import {Component, h, Prop, State} from '@stencil/core';
import {LocationSegments, RouterHistory} from "@stencil/router";
import * as collection from 'lodash/collection';
import * as array from 'lodash/array';
import {createElsaClient} from "../../../../services/elsa-client";
import {OrderBy, PagedList, VersionOptions, WorkflowBlueprintSummary, WorkflowDefinitionSummary, WorkflowInstanceSummary, WorkflowStatus} from "../../../../models";
import {DropdownButtonItem, DropdownButtonOrigin} from "../../../controls/elsa-dropdown-button/models";
import {Map, parseQuery} from '../../../../utils/utils';
import moment from "moment";

@Component({
    tag: 'elsa-workflow-instance-list-screen',
    shadow: false,
})
export class ElsaWorkflowInstanceListScreen {
    @Prop() history?: RouterHistory;
    @Prop() serverUrl: string;
    @State() workflowBlueprints: Array<WorkflowBlueprintSummary> = [];
    @State() workflowInstances: PagedList<WorkflowInstanceSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};
    @State() selectedWorkflowId?: string;
    @State() selectedWorkflowStatus?: WorkflowStatus;
    @State() selectedOrderBy?: OrderBy = OrderBy.Started;
    @State() selectedWorkflowInstanceIds: Array<string> = [];
    @State() selectAllChecked: boolean;
    @State() page: number = 0;
    @State() pageSize: number = 15;
    @State() searchTerm?: string;

    confirmDialog: HTMLElsaConfirmDialogElement;

    async componentWillLoad() {
        this.history.listen(e => this.routeChanged(e));
        this.applyQueryString(this.history.location.search);

        await this.loadWorkflowBlueprints();
        await this.loadWorkflowInstances();
    }

    applyQueryString(queryString?: string) {
        const query = parseQuery(queryString);

        this.selectedWorkflowId = query.workflow;
        this.selectedWorkflowStatus = query.status;
        this.selectedOrderBy = query.orderBy ?? OrderBy.Started;
        this.page = !!query.page ? parseInt(query.page) : 0;
        this.pageSize = !!query.pageSize ? parseInt(query.pageSize) : 15;
    }

    async loadWorkflowBlueprints() {
        const elsaClient = this.createClient();
        const versionOptions: VersionOptions = {allVersions: true};
        const workflowBlueprintPagedList = await elsaClient.workflowRegistryApi.list(null, null, versionOptions);
        this.workflowBlueprints = workflowBlueprintPagedList.items;
    }

    async loadWorkflowInstances() {
        const elsaClient = this.createClient();
        this.workflowInstances = await elsaClient.workflowInstancesApi.list(this.page, this.pageSize, this.selectedWorkflowId, this.selectedWorkflowStatus, this.selectedOrderBy, this.searchTerm);
    }

    createClient() {
        return createElsaClient(this.serverUrl);
    }

    getLatestWorkflowBlueprintVersions(): Array<WorkflowBlueprintSummary> {
        const groups = collection.groupBy(this.workflowBlueprints, 'id');
        return collection.map(groups, x => array.first(collection.sortBy(x, 'version', 'desc')));
    }

    buildFilterUrl(workflowId?: string, workflowStatus?: WorkflowStatus, orderBy?: OrderBy) {
        const filters: Map<string> = {};

        if (!!workflowId)
            filters['workflow'] = workflowId;

        if (!!workflowStatus)
            filters['status'] = workflowStatus;

        if (!!orderBy)
            filters['orderBy'] = orderBy;

        if (!!this.page)
            filters['page'] = this.page.toString();

        if (!!this.pageSize)
            filters['pageSize'] = this.pageSize.toString();

        const queryString = collection.map(filters, (v, k) => `${k}=${v}`).join('&');
        return `/workflow-instances?${queryString}`;
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

    async routeChanged(e: LocationSegments) {

        if (e.pathname.toLowerCase().indexOf('workflow-instances') < 0)
            return;

        this.applyQueryString(e.search);
        await this.loadWorkflowInstances();
    }

    onSelectAllCheckChange(e: Event) {
        const checkBox = e.target as HTMLInputElement;
        const isChecked = checkBox.checked;

        this.selectAllChecked = isChecked;
        this.selectedWorkflowInstanceIds = [];

        if (isChecked)
            this.selectedWorkflowInstanceIds = this.workflowInstances.items.map(x => x.id);
    }

    onWorkflowInstanceCheckChange(e: Event, workflowInstance: WorkflowInstanceSummary) {
        const checkBox = e.target as HTMLInputElement;
        const isChecked = checkBox.checked;

        if (isChecked)
            this.selectedWorkflowInstanceIds = [...this.selectedWorkflowInstanceIds, workflowInstance.id];
        else
            this.selectedWorkflowInstanceIds = this.selectedWorkflowInstanceIds.filter(x => x != workflowInstance.id);

        this.selectAllChecked = this.workflowInstances.items.findIndex(x => this.selectedWorkflowInstanceIds.findIndex(id => id == x.id) < 0) < 0;
    }

    async onDeleteClick(e: Event, workflowInstance: WorkflowInstanceSummary) {
        const result = await this.confirmDialog.show('Delete Workflow Instance', 'Are you sure you wish to permanently delete this workflow instance?');

        if (!result)
            return;

        const elsaClient = this.createClient();
        await elsaClient.workflowInstancesApi.delete(workflowInstance.id);
        await this.loadWorkflowInstances();
    }

    async onBulkDelete() {
        const result = await this.confirmDialog.show('Delete Selected Workflow Instances', 'Are you sure you wish to permanently delete all selected workflow instances?');

        if (!result)
            return;

        const elsaClient = this.createClient();
        await elsaClient.workflowInstancesApi.bulkDelete({workflowInstanceIds: this.selectedWorkflowInstanceIds});
        await this.loadWorkflowInstances();
    }

    async onBulkActionSelected(e: CustomEvent<DropdownButtonItem>) {
        const action = e.detail;

        switch (action.name) {
            case 'Delete':
                await this.onBulkDelete();
        }
    }

    async onSearch(e: Event) {
        e.preventDefault();
        const form = e.currentTarget as HTMLFormElement;
        const formData = new FormData(form);
        const searchTerm: FormDataEntryValue = formData.get('searchTerm');

        this.searchTerm = searchTerm.toString();
        await this.loadWorkflowInstances();
    }

    render() {
        const workflowInstances = this.workflowInstances.items;
        const workflowBlueprints = this.workflowBlueprints;

        const renderViewIcon = function () {
            return (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
            );
        };

        const renderDeleteIcon = function () {
            return (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                    <path stroke="none" d="M0 0h24v24H0z"/>
                    <line x1="4" y1="7" x2="20" y2="7"/>
                    <line x1="10" y1="11" x2="10" y2="17"/>
                    <line x1="14" y1="11" x2="14" y2="17"/>
                    <path d="M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12"/>
                    <path d="M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3"/>
                </svg>
            );
        };

        return (
            <div>
                <div class="elsa-relative elsa-z-10 elsa-flex-shrink-0 elsa-flex elsa-h-16 elsa-bg-white elsa-border-b elsa-border-gray-200">
                    <div class="elsa-flex-1 elsa-px-4 elsa-flex elsa-justify-between sm:elsa-px-6 lg:elsa-px-8">
                        <div class="elsa-flex-1 elsa-flex">
                            <form class="elsa-w-full elsa-flex md:ml-0" onSubmit={e => this.onSearch(e)}>
                                <label htmlFor="search_field" class="elsa-sr-only">Search</label>
                                <div class="elsa-relative elsa-w-full elsa-text-cool-gray-400 focus-within:elsa-text-cool-gray-600">
                                    <div class="elsa-absolute elsa-inset-y-0 elsa-left-0 elsa-flex elsa-items-center elsa-pointer-events-none">
                                        <svg class="elsa-h-5 elsa-w-5" fill="currentColor" viewBox="0 0 20 20">
                                            <path fill-rule="evenodd" clip-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"/>
                                        </svg>
                                    </div>
                                    <input name="searchTerm"
                                           class="elsa-block elsa-w-full elsa-h-full elsa-pl-8 elsa-pr-3 elsa-py-2 elsa-rounded-md elsa-text-cool-gray-900 elsa-placeholder-cool-gray-500 focus:elsa-placeholder-cool-gray-400 sm:elsa-text-sm elsa-border-0 focus:elsa-outline-none focus:elsa-ring-0"
                                           placeholder="Search"
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
                    {this.renderWorkflowFilter()}
                    {this.renderStatusFilter()}
                    {this.renderOrderByFilter()}
                </div>

                <div class="elsa-mt-8 sm:elsa-block">
                    <div class="elsa-align-middle elsa-inline-block elsa-min-w-full elsa-border-b elsa-border-gray-200">
                        <table class="elsa-min-w-full">
                            <thead>
                            <tr class="elsa-border-t elsa-border-gray-200">
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    <input type="checkbox" value="true" checked={this.selectAllChecked} onChange={e => this.onSelectAllCheckChange(e)} class="focus:elsa-ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300 elsa-rounded"/>
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    ID
                                </th>
                                <th class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Correlation ID
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Workflow
                                </th>
                                <th class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-right elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Version
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Instance Name
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Status
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Created
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Finished
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Last Executed
                                </th>
                                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                                    Faulted
                                </th>
                                <th class="elsa-pr-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider"/>
                            </tr>
                            </thead>
                            <tbody class="elsa-bg-white elsa-divide-y elsa-divide-gray-100">
                            {workflowInstances.map(workflowInstance => {
                                const workflowBlueprint = workflowBlueprints.find(x => x.id == workflowInstance.definitionId && x.version == workflowInstance.version) ?? {name: 'Not Found', displayName: '(Workflow definition not found)'};
                                const displayName = workflowBlueprint.displayName || workflowBlueprint.name || 'Untitled';
                                const statusColor = this.getStatusColor(workflowInstance.workflowStatus);
                                const viewUrl = `/workflow-instances/${workflowInstance.id}`;
                                const instanceName = !workflowInstance.name ? '' : workflowInstance.name;
                                const isSelected = this.selectedWorkflowInstanceIds.findIndex(x => x === workflowInstance.id) >= 0;
                                const createdAt = moment(workflowInstance.createdAt);
                                const finishedAt = !!workflowInstance.finishedAt ? moment(workflowInstance.finishedAt) : null;
                                const lastExecutedAt = !!workflowInstance.lastExecutedAt ? moment(workflowInstance.lastExecutedAt) : null;
                                const faultedAt = !!workflowInstance.faultedAt ? moment(workflowInstance.faultedAt) : null;

                                return <tr>
                                    <td class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                                        <input type="checkbox" value={workflowInstance.id} checked={isSelected} onChange={e => this.onWorkflowInstanceCheckChange(e, workflowInstance)}
                                               class="focus:elsa-ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300 elsa-rounded"/>
                                    </td>
                                    <td class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                                        <stencil-route-link url={viewUrl} anchorClass="elsa-truncate hover:elsa-text-gray-600">{workflowInstance.id}</stencil-route-link>
                                    </td>
                                    <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                                        {!!workflowInstance.correlationId ? workflowInstance.correlationId : ''}
                                    </td>
                                    <td class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900 elsa-text-left">
                                        <a href={`/workflow-registry/${workflowInstance.definitionId}/viewer`} class="elsa-truncate hover:elsa-text-gray-600">
                                            {displayName}
                                        </a>
                                    </td>
                                    <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-right elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-uppercase">
                                        {workflowInstance.version}
                                    </td>
                                    <td class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900 elsa-text-left">
                                        <stencil-route-link url={`"@($" workflow-registry/{workflowInstance.definitionId}/viewer")"`} anchorClass="elsa-truncate hover:elsa-text-gray-600">{instanceName}</stencil-route-link>
                                    </td>
                                    <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-right">
                                        <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                                            <div class={`flex-shrink-0 elsa-w-2-5 elsa-h-2-5 elsa-rounded-full elsa-bg-${statusColor}-600`}/>
                                            <span>{workflowInstance.workflowStatus}</span>
                                        </div>
                                    </td>
                                    <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                                        {createdAt.format('DD-MM-YYYY HH:mm:ss')}
                                    </td>
                                    <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                                        {!!finishedAt ? finishedAt.format('DD-MM-YYYY HH:mm:ss') : '-'}
                                    </td>
                                    <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                                        {!!lastExecutedAt ? lastExecutedAt.format('DD-MM-YYYY HH:mm:ss') : '-'}
                                    </td>
                                    <td class="hidden md:elsa-table-cell elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-text-left">
                                        {!!faultedAt ? faultedAt.format('DD-MM-YYYY HH:mm:ss') : '-'}
                                    </td>
                                    <td class="elsa-pr-6">
                                        <elsa-context-menu history={this.history} menuItems={[
                                            {text: 'View', anchorUrl: viewUrl, icon: renderViewIcon()},
                                            {text: 'Delete', clickHandler: e => this.onDeleteClick(e, workflowInstance), icon: renderDeleteIcon()}
                                        ]}/>
                                    </td>
                                </tr>
                            })}
                            </tbody>
                        </table>
                        <elsa-pager page={this.page} pageSize={this.pageSize} totalCount={this.workflowInstances.totalCount} history={this.history}/>
                    </div>
                    <elsa-confirm-dialog ref={el => this.confirmDialog = el}/>
                </div>
            </div>
        );
    }

    renderBulkActions() {
        const bulkActionIcon = <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M13 10V3L4 14h7v7l9-11h-7z"/>
        </svg>;

        const actions: Array<DropdownButtonItem> = [{
            text: 'Delete',
            name: 'Delete',
        }];

        return <elsa-dropdown-button text="Bulk Actions" items={actions} icon={bulkActionIcon} origin={DropdownButtonOrigin.TopLeft} onItemSelected={e => this.onBulkActionSelected(e)}/>
    }

    renderWorkflowFilter() {
        const latestWorkflowBlueprints = this.getLatestWorkflowBlueprintVersions();
        const selectedWorkflowId = this.selectedWorkflowId;
        const selectedWorkflow = latestWorkflowBlueprints.find(x => x.id == selectedWorkflowId);
        const selectedWorkflowText = !selectedWorkflowId ? "Workflow" : !!selectedWorkflow ? (selectedWorkflow.displayName || selectedWorkflow.name) : "Workflow";
        const selectedWorkflowStatus = this.selectedWorkflowStatus;
        const SelectedOrderBy = this.selectedOrderBy;
        
        let items: Array<DropdownButtonItem> = latestWorkflowBlueprints.map(x => {
            const displayName = !!x.displayName && x.displayName.length > 0 ? x.displayName : x.name || 'Untitled';
            
            return ({
                text: displayName,
                value: x.id,
                url: this.buildFilterUrl(x.id, selectedWorkflowStatus, SelectedOrderBy), isSelected: x.id == selectedWorkflowId
            });
        });

        items = [{text: 'All', value: null, url: this.buildFilterUrl(null, selectedWorkflowStatus, SelectedOrderBy), isSelected: !selectedWorkflowId}, ...items];

        const renderIcon = function () {
            return <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke="none" d="M0 0h24v24H0z"/>
                <rect x="4" y="4" width="6" height="6" rx="1"/>
                <rect x="14" y="4" width="6" height="6" rx="1"/>
                <rect x="4" y="14" width="6" height="6" rx="1"/>
                <rect x="14" y="14" width="6" height="6" rx="1"/>
            </svg>;
        };

        return <elsa-dropdown-button text={selectedWorkflowText} items={items} icon={renderIcon()} origin={DropdownButtonOrigin.TopRight} onItemSelected={e => this.selectedWorkflowId = e.detail.value as string}/>
    }

    renderStatusFilter() {
        const selectedWorkflowStatus = this.selectedWorkflowStatus;
        const selectedWorkflowStatusText = !!selectedWorkflowStatus ? selectedWorkflowStatus : 'Status';
        const statuses: Array<WorkflowStatus> = [null, WorkflowStatus.Running, WorkflowStatus.Suspended, WorkflowStatus.Finished, WorkflowStatus.Faulted, WorkflowStatus.Cancelled, WorkflowStatus.Idle];

        const items: Array<DropdownButtonItem> = statuses.map(x => {
            const text = x ?? 'All';
            return ({text: text, url: this.buildFilterUrl(this.selectedWorkflowId, x, this.selectedOrderBy), isSelected: x == selectedWorkflowStatus});
        });

        const renderIcon = function () {
            return <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <circle cx="12" cy="12" r="10"/>
                <polygon points="10 8 16 12 10 16 10 8"/>
            </svg>
        };

        return <elsa-dropdown-button text={selectedWorkflowStatusText} items={items} icon={renderIcon()} origin={DropdownButtonOrigin.TopRight}/>
    }

    renderOrderByFilter() {
        const selectedOrderBy = this.selectedOrderBy;
        const selectedOrderByText = !!selectedOrderBy ? `Sort by: ${selectedOrderBy}` : 'Sort';
        const orderByValues: Array<OrderBy> = [OrderBy.Finished, OrderBy.LastExecuted, OrderBy.Started];

        const items: Array<DropdownButtonItem> = orderByValues.map(x => {
            return ({text: x, url: this.buildFilterUrl(this.selectedWorkflowId, this.selectedWorkflowStatus, x), isSelected: x == selectedOrderBy});
        });

        const renderIcon = function () {
            return <svg class="elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4h13M3 8h9m-9 4h9m5-4v12m0 0l-4-4m4 4l4-4"/>
            </svg>
        };

        return <elsa-dropdown-button text={selectedOrderByText} items={items} icon={renderIcon()} origin={DropdownButtonOrigin.TopRight}/>
    }
}
