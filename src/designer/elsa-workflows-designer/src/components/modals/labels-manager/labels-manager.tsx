import {Component, h, Host, Method, State} from '@stencil/core';
import {DefaultActions, Label} from "../../../models";
import {Container} from "typedi";
import {ElsaApiClientProvider, ElsaClient} from "../../../services";

@Component({
  tag: 'elsa-labels-manager',
  shadow: false,
})
export class LabelsManager {
  private elsaClient: ElsaClient;
  private modalDialog: HTMLElsaModalDialogElement;

  @State() private labels: Array<Label> = [];

  @Method()
  public async show() {
    await this.modalDialog.show();
    await this.loadLabels();
  }

  @Method()
  public async hide() {
    await this.modalDialog.hide();
  }

  public async componentWillLoad() {
    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getClient();
  }

  private async onDeleteClick(e: MouseEvent, label: Label) {
    const elsaClient = this.elsaClient;
    await this.loadLabels();
  }

  private async loadLabels() {
    const elsaClient = this.elsaClient;

  }

  render() {

    const closeAction = DefaultActions.Close();
    const actions = [closeAction];

    const icon = () => <svg class="h-6 w-6 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <circle cx="12" cy="12" r="10"/>
      <rect x="9" y="9" width="6" height="6"/>
    </svg>

    return (
      <Host class="block">

        <elsa-modal-dialog ref={el => this.modalDialog = el} actions={actions}>
          <div class="pt-4">
            <h2 class="text-lg font-medium ml-4 mb-2">Labels</h2>

            <div class="pl-4 pr-6 ">
              <div class="flex">
                <div class="flex-grow">
                  <p class="mt-1 max-w-2xl text-sm text-gray-500">30 labels</p>
                </div>
                <div class="flex-shrink">
                  <button type="button" class="btn">New label</button>
                </div>
              </div>

              <div class="mt-5 bg-gray-50 sm:rounded-lg">
                <form class="p-5 space-y-8 divide-y divide-gray-200">
                  <div class="mt-4 grid grid-cols-3 gap-x-4">
                    <div>
                      <label htmlFor="first-name" class="block text-sm font-medium text-gray-700">Name</label>
                      <div class="mt-1">
                        <input type="text" id="first-name" name="first-name" autoComplete="given-name" class="block border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"/>
                      </div>
                    </div>

                    <div>
                      <label htmlFor="last-name" class="block text-sm font-medium text-gray-700">Description</label>
                      <div class="mt-1">
                        <input type="text" id="last-name" name="last-name" autoComplete="family-name" class="block border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"/>
                      </div>
                    </div>

                    <div>
                      <label htmlFor="last-name" class="block text-sm font-medium text-gray-700">Color</label>
                      <div class="mt-1">
                        <input type="text" id="last-name" name="last-name" autoComplete="family-name" class="block border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"/>
                      </div>
                    </div>

                  </div>

                  <div class="pt-5">
                    <div class="flex justify-end">
                      <button type="button"
                              class="bg-white py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">Cancel
                      </button>
                      <button type="submit"
                              class="ml-3 inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">Create
                      </button>
                    </div>
                  </div>
                </form>
              </div>

              <div class="mt-5 border-t border-gray-200">
                <dl class="divide-y divide-gray-200">
                  <div class="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4">
                    <dt>
                      <span class="inline-flex items-center px-3 py-0.5 rounded-full text-sm font-medium bg-red-100 text-red-800"> Badge </span>
                    </dt>
                    <dd class="mt-1 flex text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      <span class="flex-grow text-sm">
                        Description
                      </span>
                      <span class="ml-4 flex-grow">
                        <a href="#" title="Linked to 20 workflow definitions" class="inline-flex items-center space-x-2">
                          {icon()} <span>20</span>
                        </a>
                      </span>
                      <span class="ml-4 flex-shrink-0">
                        <span class="relative z-0 inline-flex shadow-sm rounded-md">
                          <button type="button"
                                  class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500">Edit</button>
                          <button type="button"
                                  class="-ml-px relative inline-flex items-center px-4 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500">Delete</button>
                        </span>
                      </span>
                    </dd>
                  </div>
                  <div class="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4">
                    <dt>
                      <span class="inline-flex items-center px-3 py-0.5 rounded-full text-sm font-medium bg-red-100 text-red-800"> Badge </span>
                    </dt>
                    <dd class="mt-1 flex text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      <span class="flex-grow">
                        Description
                      </span>
                      <span class="ml-4 flex-grow">
                        <a href="#" title="Linked to 20 workflow definitions" class="inline-flex items-center space-x-2">
                          {icon()} <span>20</span>
                        </a>
                      </span>
                      <span class="ml-4 flex-shrink-0">
                        <span class="relative z-0 inline-flex shadow-sm rounded-md">
                          <button type="button"
                                  class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500">Edit</button>
                          <button type="button"
                                  class="-ml-px relative inline-flex items-center px-4 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500">Delete</button>
                        </span>
                      </span>
                    </dd>
                  </div>
                </dl>
              </div>

            </div>
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }
}

