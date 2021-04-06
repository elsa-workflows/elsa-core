import {Component, h, Prop, State} from '@stencil/core';
import * as collection from 'lodash/collection';
import * as object from 'lodash/object';
import {createElsaClient} from "../../../../services/elsa-client";
import {PagedList, VersionOptions, WorkflowDefinitionSummary} from "../../../../models";

@Component({
  tag: 'elsa-workflow-definitions-list-screen',
  styleUrl: 'elsa-workflow-definitions-list-screen.css',
  shadow: false,
})
export class ElsaWorkflowDefinitionsListScreen {

  @Prop() serverUrl: string;
  @State() workflowDefinitions: PagedList<WorkflowDefinitionSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};

  confirmDialog: HTMLElsaConfirmDialogElement;

  async componentWillLoad() {
    await this.loadWorkflowDefinitions();
  }

  async onDeleteClick(e: Event, workflowDefinition: WorkflowDefinitionSummary) {
    const result = await this.confirmDialog.show('Delete Workflow Definition', 'Are you sure you wish to permanently delete this workflow, including all of its workflow instances?');

    if (!result)
      return;

    const elsaClient = this.createClient();
    await elsaClient.workflowDefinitionsApi.delete(workflowDefinition.definitionId);
    await this.loadWorkflowDefinitions();
  }

  async loadWorkflowDefinitions() {
    const elsaClient = this.createClient();
    const page = 0;
    const pageSize = 50;
    const versionOptions: VersionOptions = {allVersions: true};
    this.workflowDefinitions = await elsaClient.workflowDefinitionsApi.list(page, pageSize, versionOptions);
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  render() {
    const workflowDefinitions = this.workflowDefinitions.items;
    const groupings = collection.groupBy(workflowDefinitions, 'definitionId');

    return (
      <div>
        <div class="align-middle inline-block min-w-full border-b border-gray-200">
          <table class="min-w-full">
            <thead>
            <tr class="border-t border-gray-200">
              <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider"><span class="lg:pl-2">Name</span></th>
              <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                Instances
              </th>
              <th class="hidden md:table-cell px-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                Latest Version
              </th>
              <th class="hidden md:table-cell px-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                Published Version
              </th>
              <th class="pr-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider"/>
            </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-100">
            {collection.map(groupings, group => {
              const versions = collection.orderBy(group, 'version', 'desc');
              const workflowDefinition = versions[0]
              const latestVersionNumber = workflowDefinition.version;
              const publishedVersion: WorkflowDefinitionSummary = versions.find(x => x.isPublished);
              const publishedVersionNumber = !!publishedVersion ? publishedVersion.version : '-';
              let workflowDisplayName = workflowDefinition.displayName;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = workflowDefinition.name;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = 'Untitled';

              const editUrl = `/workflow-definitions/${workflowDefinition.definitionId}`;

              const editIcon = (
                <svg class="h-5 w-5 text-gray-500" width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
              );

              const deleteIcon = (
                <svg class="h-5 w-5 text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                  <path stroke="none" d="M0 0h24v24H0z"/>
                  <line x1="4" y1="7" x2="20" y2="7"/>
                  <line x1="10" y1="11" x2="10" y2="17"/>
                  <line x1="14" y1="11" x2="14" y2="17"/>
                  <path d="M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12"/>
                  <path d="M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3"/>
                </svg>
              );

              return (
                <tr>
                  <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900">
                    <div class="flex items-center space-x-3 lg:pl-2">
                      <stencil-route-link url={editUrl} anchorClass="truncate hover:text-gray-600"><span>{workflowDisplayName}</span></stencil-route-link>
                    </div>
                  </td>

                  <td class="px-6 py-3 text-sm leading-5 text-gray-500 font-medium">
                    <div class="flex items-center space-x-2">
                      <div class="flex items-center space-x-2">
                        <div class="flex flex-shrink-0 -space-x-1"><a class="max-w-none h-9 w-9 rounded-full text-white shadow-solid p-2 text-xs bg-blue-500 hover:bg-blue-400" href="#">4</a>
                          <a class="max-w-none h-9 w-9 rounded-full text-white shadow-solid p-2 text-xs bg-green-500 hover:bg-green-400" href="#">999</a>
                          <a class="max-w-none h-9 w-9 rounded-full text-white shadow-solid p-2 text-xs bg-red-500 hover:bg-red-400" href="#">75</a></div>
                      </div>
                    </div>
                  </td>
                  <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">{latestVersionNumber}</td>
                  <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">{publishedVersionNumber}</td>
                  <td class="pr-6">
                    <elsa-context-menu menuItems={[
                      {text: 'Edit', anchorUrl: editUrl, icon: editIcon},
                      {text: 'Delete', clickHandler: e => this.onDeleteClick(e, workflowDefinition), icon: deleteIcon}
                    ]}/>
                  </td>
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
