import {Component, h, Prop, State} from '@stencil/core';
import {LocationSegments, RouterHistory} from "@stencil/router";
import * as collection from 'lodash/collection';
import * as array from 'lodash/array';
import {createElsaClient} from "../../../../services/elsa-client";
import {OrderBy, PagedList, VersionOptions, WorkflowBlueprintSummary, WorkflowDefinitionSummary, WorkflowInstanceSummary, WorkflowStatus} from "../../../../models";
import {DropdownButtonItem, DropdownButtonOrigin} from "../../../controls/elsa-dropdown-button/models";
import {Map, parseQuery} from '../../../../utils/utils';

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
                return "pink";
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
                <svg class="h-5 w-5 text-gray-500" width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
            );
        };

        const renderDeleteIcon = function () {
            return (
                <svg class="h-5 w-5 text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
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

                <div class="relative z-10 flex-shrink-0 flex h-16 bg-white border-b border-gray-200">
                    <div class="flex-1 px-4 flex justify-between sm:px-6 lg:px-8">
                        <div class="flex-1 flex">
                            <form class="w-full flex md:ml-0" onSubmit={e => this.onSearch(e)}>
                                <label htmlFor="search_field" class="sr-only">Search</label>
                                <div class="relative w-full text-cool-gray-400 focus-within:text-cool-gray-600">
                                    <div class="absolute inset-y-0 left-0 flex items-center pointer-events-none">
                                        <svg class="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                                            <path fill-rule="evenodd" clip-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"/>
                                        </svg>
                                    </div>
                                    <input name="searchTerm"
                                           class="block w-full h-full pl-8 pr-3 py-2 rounded-md text-cool-gray-900 placeholder-cool-gray-500 focus:placeholder-cool-gray-400 sm:text-sm border-0 focus:outline-none focus:ring-0"
                                           placeholder="Search"
                                           type="search"/>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>

                <div class="p-8 flex content-end justify-right bg-white space-x-4">
                    <div class="flex-shrink-0">
                        {this.renderBulkActions()}
                    </div>
                    <div class="flex-1">
                        &nbsp;
                    </div>
                    {this.renderWorkflowFilter()}
                    {this.renderStatusFilter()}
                    {this.renderOrderByFilter()}
                </div>

                <div class="mt-8 sm:block">
                    <div class="align-middle inline-block min-w-full border-b border-gray-200">
                        <table class="min-w-full">
                            <thead>
                            <tr class="border-t border-gray-200">
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    <input type="checkbox" value="true" checked={this.selectAllChecked} onChange={e => this.onSelectAllCheckChange(e)} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    <span class="lg:pl-2">ID</span>
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Workflow
                                </th>
                                <th class="hidden md:table-cell px-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Version
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Instance Name
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Status
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Created
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Finished
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Last Executed
                                </th>
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    Faulted
                                </th>
                                <th class="pr-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider"/>
                            </tr>
                            </thead>
                            <tbody class="bg-white divide-y divide-gray-100">
                            {workflowInstances.map(workflowInstance => {
                                const workflowBlueprint = workflowBlueprints.find(x => x.id == workflowInstance.definitionId && x.version == workflowInstance.version) ?? {displayName: '(Workflow definition not found)'};
                                const displayName = workflowBlueprint.displayName;
                                const statusColor = this.getStatusColor(workflowInstance.workflowStatus);
                                const viewUrl = `/workflow-instances/${workflowInstance.id}`;
                                const instanceName = !workflowInstance.name ? '' : workflowInstance.name;
                                const isSelected = this.selectedWorkflowInstanceIds.findIndex(x => x === workflowInstance.id) >= 0;
                                //var displayContext = WorkflowInstanceDisplayContexts[workflowInstance];

                                return <tr>
                                    <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900">
                                        <input type="checkbox" value={workflowInstance.id} checked={isSelected} onChange={e => this.onWorkflowInstanceCheckChange(e, workflowInstance)}
                                               class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
                                    </td>
                                    <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900">
                                        <stencil-route-link url={viewUrl} anchorClass="truncate hover:text-gray-600">{workflowInstance.id}</stencil-route-link>
                                    </td>
                                    <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900 text-left">
                                        <a href={`/workflow-registry/${workflowInstance.definitionId}/viewer`} class="truncate hover:text-gray-600">
                                            {displayName}
                                        </a>
                                    </td>
                                    <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">
                                        {workflowInstance.version}
                                    </td>
                                    <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900 text-left">
                                        <stencil-route-link url={`"@($" workflow-registry/{workflowInstance.definitionId}/viewer")"`} anchorClass="truncate hover:text-gray-600">{instanceName}</stencil-route-link>
                                    </td>
                                    <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">
                                        <div class="flex items-center space-x-3 lg:pl-2">
                                            <div class={`flex-shrink-0 w-2-5 h-2-5 rounded-full bg-${statusColor}-600`}/>
                                            <span>{workflowInstance.workflowStatus}</span>
                                        </div>
                                    </td>
                                    <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-left">
                                        {workflowInstance.createdAt}
                                    </td>
                                    <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-left">
                                        {!!workflowInstance.finishedAt ? workflowInstance.finishedAt.toString() : '-'}
                                    </td>
                                    <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-left">
                                        {!!workflowInstance.lastExecutedAt ? workflowInstance.lastExecutedAt.toString() : '-'}
                                    </td>
                                    <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-left">
                                        {!!workflowInstance.faultedAt ? workflowInstance.faultedAt.toString() : '-'}
                                    </td>
                                    <td class="pr-6">
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
        const bulkActionIcon = <svg class="mr-3 h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
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
            return <svg class="mr-3 h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
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
            return <svg class="mr-3 h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
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
            return <svg class="mr-3 h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4h13M3 8h9m-9 4h9m5-4v12m0 0l-4-4m4 4l4-4"/>
            </svg>
        };

        return <elsa-dropdown-button text={selectedOrderByText} items={items} icon={renderIcon()} origin={DropdownButtonOrigin.TopRight}/>
    }
}
