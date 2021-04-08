import {Component, h, Prop, State} from '@stencil/core';
import {LocationSegments, RouterHistory} from "@stencil/router";
import * as collection from 'lodash/collection';
import * as array from 'lodash/array';
import {createElsaClient} from "../../../../services/elsa-client";
import {OrderBy, PagedList, VersionOptions, WorkflowBlueprintSummary, WorkflowInstanceSummary, WorkflowStatus} from "../../../../models";
import {DropdownButtonItem, DropdownButtonOrigin} from "../../../controls/elsa-dropdown-button/models";
import {Map, parseQuery} from '../../../../utils/utils';
import {parseQueryString} from "@stencil/router/dist/types/utils/path-utils";

@Component({
    tag: 'elsa-workflow-instances-list-screen',
    styleUrl: 'elsa-workflow-instances-list-screen.css',
    shadow: false,
})
export class ElsaWorkflowInstancesListScreen {
    @Prop() history?: RouterHistory;
    @Prop() serverUrl: string;
    @State() workflowBlueprints: Array<WorkflowBlueprintSummary> = [];
    @State() workflowInstances: PagedList<WorkflowInstanceSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};
    @State() selectedWorkflowId?: string;
    @State() selectedWorkflowStatus?: WorkflowStatus;
    @State() selectedOrderBy?: OrderBy;
    @State() selectedWorkflowInstanceIds: Array<string> = [];

    confirmDialog: HTMLElsaConfirmDialogElement;

    async componentWillLoad() {
        this.history.listen(e => this.routeChanged(e));
        this.applyQueryString(this.history.location.search);

        await this.loadWorkflowBlueprints();
        await this.loadWorkflowInstances();
    }

    async routeChanged(e: LocationSegments) {
        if(!e.pathname.toLowerCase().indexOf('workflow-instances'))
            return;
        
        this.applyQueryString(e.search);
        await this.loadWorkflowInstances();
    }
    
    applyQueryString(queryString?: string){
        const query = parseQuery(queryString);
        
        this.selectedWorkflowId = !!query.workflow ? query.workflow : null;
        this.selectedWorkflowStatus = !!query.status ? this.parseStatusText(query.status) : null;
    }

    async loadWorkflowBlueprints() {
        const elsaClient = this.createClient();
        const versionOptions: VersionOptions = {isLatest: true};
        const workflowBlueprintPagedList = await elsaClient.workflowRegistryApi.list(null, null, versionOptions);
        this.workflowBlueprints = workflowBlueprintPagedList.items;
    }

    async loadWorkflowInstances() {
        const elsaClient = this.createClient();
        const page = 1;
        const pageSize = 25;
        
        this.workflowInstances = await elsaClient.workflowInstancesApi.list(page, pageSize, this.selectedWorkflowId, this.selectedWorkflowStatus, this.selectedOrderBy);
    }

    createClient() {
        return createElsaClient(this.serverUrl);
    }

    getLatestWorkflowBlueprintVersions(): Array<WorkflowBlueprintSummary> {
        const groups = collection.groupBy(this.workflowBlueprints, 'id');
        return collection.map(groups, x => array.first(collection.sortBy(x, 'version', 'desc')));
    }

    buildFilterUrl(workflowId?: string, workflowStatus?: WorkflowStatus, orderBy?: OrderBy) {
        const workflowStatusText = workflowStatus != null ? this.getStatusText(workflowStatus) : null;
        const orderByText = !!orderBy ? orderBy.toString() : null;

        const filters: Map<string> = {};

        if (!!workflowId)
            filters['workflow'] = workflowId;

        if (!!workflowStatusText)
            filters['status'] = workflowStatusText;

        if (!!orderByText)
            filters['orderBy'] = orderByText;

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
    
    getStatusText(status: WorkflowStatus){
        switch (status) {
            default:
            case WorkflowStatus.Idle:
                return "Idle";
            case WorkflowStatus.Running:
                return "Running";
            case WorkflowStatus.Suspended:
                return "Suspended";
            case WorkflowStatus.Finished:
                return "Finished";
            case WorkflowStatus.Faulted:
                return "Faulted";
            case WorkflowStatus.Cancelled:
                return "Cancelled";
        }
    }

    parseStatusText(status?: string): WorkflowStatus {
        if(!status)
            return null;
            
        switch (status.toLowerCase()) {
            default:
            case 'idle':
                return WorkflowStatus.Idle;
            case 'running':
                return WorkflowStatus.Running;
            case 'suspended':
                return WorkflowStatus.Suspended;
            case 'finished':
                return WorkflowStatus.Finished;
            case 'faulted':
                return WorkflowStatus.Faulted;
            case 'cancelled':
                return WorkflowStatus.Cancelled;
        }
    }

    render() {
        const workflowInstances = this.workflowInstances.items;
        const workflowBlueprints = this.workflowBlueprints;

        return (
            <div>
                <div class="p-8 flex content-end justify-right bg-white space-x-4">
                    <div class="flex-shrink-0">
                        {this.renderBulkActions()}
                    </div>
                    <div class="flex-1">
                        &nbsp;
                    </div>
                    {this.renderWorkflowFilter()}
                    {this.renderStatusFilter()}
                    <elsa-dropdown-button text="@SelectedOrderByText" items={[]} icon={null} origin={DropdownButtonOrigin.TopRight}/>
                </div>
                
                <div class="mt-8 sm:block">
                    <div class="align-middle inline-block min-w-full border-b border-gray-200">
                        <table class="min-w-full">
                            <thead>
                            <tr class="border-t border-gray-200">
                                <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                                    <input type="checkbox" value="true" checked={false} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
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
                                const viewUrl = `/workflow-instances/${workflowInstance.id}/viewer`;
                                const instanceName = !workflowInstance.name ? '' : workflowInstance.name;
                                const isSelected = this.selectedWorkflowInstanceIds.findIndex(x => x === workflowInstance.id) >= 0;
                                //var displayContext = WorkflowInstanceDisplayContexts[workflowInstance];

                                return <tr>
                                    <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900">
                                        <input type="checkbox" value={workflowInstance.id} checked={isSelected} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
                                    </td>
                                    <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900">
                                        <a href={viewUrl} class="truncate hover:text-gray-600">{workflowInstance.id}</a>
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
                                        <a href={`"@($" workflow-registry/{workflowInstance.DefinitionId}/viewer")"`} class="truncate hover:text-gray-600">{instanceName}</a>
                                    </td>
                                    <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">
                                        <div class="flex items-center space-x-3 lg:pl-2">
                                            <div class="flex-shrink-0 w-2.5 h-2.5 rounded-full bg-@statusColor-600"/>
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
                                        <elsa-context-menu/>
                                    </td>
                                </tr>
                            })}
                            </tbody>
                        </table>
                        <elsa-pager page="@WorkflowInstances.Page" pageSize="@WorkflowInstances.PageSize" totalCount="@WorkflowInstances.TotalCount"/>
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
            name: 'Delete'
        }];

        return <elsa-dropdown-button text="Bulk Actions" items={actions} icon={bulkActionIcon} origin={DropdownButtonOrigin.TopLeft}/>
    }

    renderWorkflowFilter() {
        const latestWorkflowBlueprints = this.getLatestWorkflowBlueprintVersions();
        const selectedWorkflowId = this.selectedWorkflowId;
        const selectedWorkflow = latestWorkflowBlueprints.find(x => x.id == selectedWorkflowId);
        const selectedWorkflowText = !selectedWorkflowId ? "Workflow" : !!selectedWorkflow ? (selectedWorkflow.displayName || selectedWorkflow.name) : "Workflow";
        const selectedWorkflowStatus = this.selectedWorkflowStatus;
        const SelectedOrderBy = this.selectedOrderBy;
        let items: Array<DropdownButtonItem> = latestWorkflowBlueprints.map(x => ({text: x.displayName!, value: x.id, url: this.buildFilterUrl(x.id, selectedWorkflowStatus, SelectedOrderBy), isSelected: x.id == selectedWorkflowId}));

        items = [{text: 'All', value: null, url: this.buildFilterUrl(null, selectedWorkflowStatus, SelectedOrderBy), isSelected: !selectedWorkflowId}, ...items];

        return <elsa-dropdown-button text={selectedWorkflowText} items={items} icon={null} origin={DropdownButtonOrigin.TopRight} onItemSelected={e => this.selectedWorkflowId = e.detail.value as string}/>
    }
    
    renderStatusFilter(){
        const selectedWorkflowStatus = this.selectedWorkflowStatus;
        const selectedWorkflowStatusText = selectedWorkflowStatus != null ? this.getStatusText(selectedWorkflowStatus) : 'Status';
        const statuses: Array<WorkflowStatus> = [null, WorkflowStatus.Running, WorkflowStatus.Suspended, WorkflowStatus.Finished, WorkflowStatus.Faulted, WorkflowStatus.Cancelled, WorkflowStatus.Idle];
        const items: Array<DropdownButtonItem> = statuses.map(x => {
            const enumText = x != null ? this.getStatusText(x) : null;
            const text = !!enumText ? enumText : 'All';
            return ({text: text, url: this.buildFilterUrl(this.selectedWorkflowId, x, this.selectedOrderBy), isSelected: x == selectedWorkflowStatus});
        });
        
        return <elsa-dropdown-button text={selectedWorkflowStatusText} items={items} icon={null} origin={DropdownButtonOrigin.TopRight}/>
    }
}
