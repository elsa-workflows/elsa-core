import {Component, h, Prop, State} from '@stencil/core';
import * as collection from 'lodash/collection';
import {createElsaClient} from "../../../../services/elsa-client";
import {PagedList, VersionOptions, WorkflowBlueprintSummary} from "../../../../models";
import {RouterHistory} from "@stencil/router";

@Component({
  tag: 'elsa-workflow-registry-list-screen',
  shadow: false,
})
export class ElsaWorkflowRegistryListScreen {
  @Prop() history?: RouterHistory;
  @Prop() serverUrl: string;
  @State() workflowBlueprints: PagedList<WorkflowBlueprintSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};

  async componentWillLoad() {
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
              const workflowBlueprint: WorkflowBlueprintSummary = versions[0];
              const latestVersionNumber = workflowBlueprint.version;
              const publishedVersion: WorkflowBlueprintSummary = versions.find(x => x.isPublished);
              const publishedVersionNumber = !!publishedVersion ? publishedVersion.version : '-';
              let workflowDisplayName = workflowBlueprint.displayName;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = workflowBlueprint.name;

              if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                workflowDisplayName = 'Untitled';

              const editUrl = `/workflow-registry/${workflowBlueprint.id}`;
              const instancesUrl = `/workflow-instances?workflow=${workflowBlueprint.id}`;

              const editIcon = (
                <svg class="h-5 w-5 text-gray-500" width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
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
                    <div class="flex items-center space-x-3 lg:pl-2">
                      <stencil-route-link url={instancesUrl} anchorClass="truncate hover:text-gray-600">Instances</stencil-route-link>
                    </div>
                  </td>
                  
                  <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">{latestVersionNumber}</td>
                  <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">{publishedVersionNumber}</td>
                  <td class="pr-6">
                    <elsa-context-menu history={this.history} menuItems={[
                      {text: 'Edit', anchorUrl: editUrl, icon: editIcon},
                    ]}/>
                  </td>
                </tr>
              );
            })}
            </tbody>
          </table>
        </div>

      </div>
    );
  }
}
