import {Component, h, Host, Prop} from '@stencil/core';
import {WorkflowDefinition} from "../../../../models";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-version-history-panel',
  shadow: false
})
export class ElsaVersionHistoryPanel {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() serverUrl: string;

  confirmDialog: HTMLElsaConfirmDialogElement;

  render() {

    return (
      <Host>
        <dl class="elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-text-sm elsa-font-medium">

            <div class="elsa-mt-2 elsa-flex elsa-flex-col">
              <div class="elsa-overflow-x-auto">
                <div class="elsa-inline-block elsa-min-w-full elsa-py-2 elsa-align-middle">
                  <div class="elsa-overflow-hidden elsa-shadow-sm elsa-ring-1 elsa-ring-black elsa-ring-opacity-5 md:elsa-rounded-lg">
                    <table class="elsa-min-w-full elsa-divide-y elsa-divide-gray-300">
                      <thead class="elsa-bg-gray-50">
                      <tr>
                        <th scope="col" class="elsa-py-3.5 elsa-pl-4 elsa-pr-3 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900 sm:elsa-pl-6 lg:elsa-pl-8">Version</th>
                        <th scope="col" class="elsa-px-3 elsa-py-3.5 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900">Created</th>
                        <th scope="col" class="elsa-relative elsa-py-3.5 elsa-pl-3 elsa-pr-4 sm:elsa-pr-6 lg:elsa-pr-8">
                          <span class="elsa-sr-only">View</span>
                        </th>
                      </tr>
                      </thead>
                      <tbody class="elsa-divide-y elsa-divide-gray-200 elsa-bg-white">
                      {[0, 0, 0, 0, 0].map(v => (
                          <tr>
                            <td class="elsa-whitespace-nowrap elsa-py-4 elsa-pl-4 elsa-pr-3 elsa-text-sm elsa-font-medium elsa-text-gray-900 sm:elsa-pl-6 lg:elsa-pl-8">8</td>
                            <td class="elsa-whitespace-nowrap elsa-px-3 elsa-py-4 elsa-text-sm elsa-text-gray-500">10-12-2020 13:20</td>
                            <td class="elsa-relative elsa-whitespace-nowrap elsa-py-4 elsa-pl-3 elsa-pr-4 elsa-text-right elsa-text-sm elsa-font-medium sm:elsa-pr-6 lg:elsa-pr-8">
                              <a href="#" class="elsa-text-blue-600 hover:elsa-text-blue-900">View<span class="elsa-sr-only">, 8</span></a>
                            </td>
                          </tr>
                        )
                      )}

                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>


          </div>
        </dl>
        <elsa-confirm-dialog ref={el => this.confirmDialog = el}/>
      </Host>
    );
  }
}

Tunnel.injectProps(ElsaVersionHistoryPanel, ['serverUrl']);
