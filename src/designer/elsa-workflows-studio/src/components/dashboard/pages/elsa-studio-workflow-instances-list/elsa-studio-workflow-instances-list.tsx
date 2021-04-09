import {Component, h, Prop, State} from '@stencil/core';
import {RouterHistory} from "@stencil/router";

@Component({
  tag: 'elsa-studio-workflow-instances-list',
  shadow: false,
})
export class ElsaStudioWorkflowInstancesList {
  @Prop() history: RouterHistory;
  @Prop() serverUrl: string;

  render() {
    return (
      <div>
        <div class="border-b border-gray-200 px-4 py-4 sm:flex sm:items-center sm:justify-between sm:px-6 lg:px-8 bg-white">
          <div class="flex-1 min-w-0">
            <h1 class="text-lg font-medium leading-6 text-gray-900 sm:truncate">
              Workflow Instances
            </h1>
          </div>
        </div>

        <elsa-workflow-instances-list-screen history={this.history} serverUrl={this.serverUrl} />
      </div>
    );
  }
}
