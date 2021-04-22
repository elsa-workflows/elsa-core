import {Component, h, Prop} from '@stencil/core';
import {RouterHistory} from "@stencil/router";

@Component({
  tag: 'elsa-studio-workflow-registry',
  shadow: false,
})
export class ElsaStudioWorkflowRegistry {
  @Prop() history: RouterHistory;
  @Prop() serverUrl: string;
  
  render() {
    return (
      <div>
        <div class="border-b border-gray-200 px-4 py-4 sm:flex sm:items-center sm:justify-between sm:px-6 lg:px-8 bg-white">
          <div class="flex-1 min-w-0">
            <h1 class="text-lg font-medium leading-6 text-gray-900 sm:truncate">
              Workflow Registry
            </h1>
          </div>
          <div class="mt-4 flex sm:mt-0 sm:ml-4">
            <stencil-route-link url="/workflow-definitions/new" class="order-0 inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:order-1 sm:ml-3">
              Create Workflow
            </stencil-route-link>
          </div>
        </div>

        <elsa-workflow-registry-list-screen history={this.history} serverUrl={this.serverUrl} />
      </div>
    );
  }
}
