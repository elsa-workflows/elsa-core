import {Component, Host, h, Prop, State} from '@stencil/core';
import {eventBus} from '../../../../utils/event-bus';
import {EventTypes} from "../../../../models/events";
import state from '../../../../utils/store';

@Component({
  tag: 'elsa-activity-picker-modal',
  styleUrl: 'elsa-activity-picker-modal.css',
  shadow: false,
})
export class ElsaActivityPickerModal {

  dialog: HTMLElsaModalDialogElement

  componentDidLoad() {
    eventBus.on(EventTypes.ShowActivityPicker, async () => await this.dialog.show());
  }

  render() {
    return this.renderModal();
  }

  renderModal() {
    const activityDescriptors = state.activityDescriptors;

    return (
      <Host>
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="py-8">
            <div class="flex">
              <div class="px-8">
                <nav class="space-y-1" aria-label="Sidebar">
                  <a href="#"
                     class="bg-gray-100 text-gray-900 flex items-center px-3 py-2 text-sm font-medium rounded-md"
                     aria-current="page">
                    <span class="truncate">
                      All
                    </span>
                  </a>

                  <a href="#"
                     class="text-gray-600 hover:bg-gray-50 hover:text-gray-900 flex items-center px-3 py-2 text-sm font-medium rounded-md">
                    <span class="truncate">
                      Actions
                    </span>
                  </a>

                  <a href="#"
                     class="text-gray-600 hover:bg-gray-50 hover:text-gray-900 flex items-center px-3 py-2 text-sm font-medium rounded-md">
                    <span class="truncate">
                      Triggers
                    </span>
                  </a>
                </nav>
              </div>
              <div class="flex-1 pr-8">

                <div class="p-0 mb-6">
                  <div class="relative rounded-md shadow-sm">
                    <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <svg class="h-6 w-6 text-gray-400" width="24" height="24" viewBox="0 0 24 24"
                           stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round"
                           stroke-linejoin="round">
                        <path stroke="none" d="M0 0h24v24H0z"/>
                        <circle cx="10" cy="10" r="7"/>
                        <line x1="21" y1="21" x2="15" y2="15"/>
                      </svg>
                    </div>
                    <input type="text" class="form-input block w-full pl-10 sm:text-sm sm:leading-5" placeholder="Search activities"/>
                  </div>
                </div>

                <div class="max-w-4xl mx-auto p-0">
                  <div class="rounded-lg bg-gray-200 overflow-hidden shadow divide-y divide-gray-200 sm:divide-y-0 sm:grid sm:grid-cols-2 sm:gap-px">
                    <div class="rounded-tl-lg rounded-tr-lg sm:rounded-tr-none relative group bg-white p-6 focus-within:ring-2 focus-within:ring-inset focus-within:ring-indigo-500">
                      <div>
                        <span class="rounded-lg inline-flex p-3 bg-teal-50 text-teal-700 ring-4 ring-white">
                          <svg class="h-6 w-6" x-description="Heroicon name: outline/clock" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                          </svg>
                        </span>
                      </div>
                      <div class="mt-8">
                        <h3 class="text-lg font-medium">
                          <a href="#" class="focus:outline-none">
                            <span class="absolute inset-0" aria-hidden="true"/>
                            Request time off
                          </a>
                        </h3>
                        <p class="mt-2 text-sm text-gray-500">
                          Doloribus dolores nostrum quia qui natus officia quod et dolorem. Sit repellendus qui ut at
                          blanditiis et quo et molestiae.
                        </p>
                      </div>
                      <span class="pointer-events-none absolute top-6 right-6 text-gray-300 group-hover:text-gray-400" aria-hidden="true">
                        <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 24 24">
                          <path
                            d="M20 4h1a1 1 0 00-1-1v1zm-1 12a1 1 0 102 0h-2zM8 3a1 1 0 000 2V3zM3.293 19.293a1 1 0 101.414 1.414l-1.414-1.414zM19 4v12h2V4h-2zm1-1H8v2h12V3zm-.707.293l-16 16 1.414 1.414 16-16-1.414-1.414z"/>
                        </svg>
                      </span>
                    </div>

                    <div class="sm:rounded-tr-lg   relative group bg-white p-6 focus-within:ring-2 focus-within:ring-inset focus-within:ring-indigo-500">
                      <div>
          <span class="rounded-lg inline-flex p-3 bg-purple-50 text-purple-700 ring-4 ring-white">
            <svg class="h-6 w-6" x-description="Heroicon name: outline/badge-check" xmlns="http://www.w3.org/2000/svg"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
        d="M9 12l2 2 4-4M7.835 4.697a3.42 3.42 0 001.946-.806 3.42 3.42 0 014.438 0 3.42 3.42 0 001.946.806 3.42 3.42 0 013.138 3.138 3.42 3.42 0 00.806 1.946 3.42 3.42 0 010 4.438 3.42 3.42 0 00-.806 1.946 3.42 3.42 0 01-3.138 3.138 3.42 3.42 0 00-1.946.806 3.42 3.42 0 01-4.438 0 3.42 3.42 0 00-1.946-.806 3.42 3.42 0 01-3.138-3.138 3.42 3.42 0 00-.806-1.946 3.42 3.42 0 010-4.438 3.42 3.42 0 00.806-1.946 3.42 3.42 0 013.138-3.138z"/>
</svg>
          </span>
                      </div>
                      <div class="mt-8">
                        <h3 class="text-lg font-medium">
                          <a href="#" class="focus:outline-none">

                            <span class="absolute inset-0" aria-hidden="true"/>
                            Benefits
                          </a>
                        </h3>
                        <p class="mt-2 text-sm text-gray-500">
                          Doloribus dolores nostrum quia qui natus officia quod et dolorem. Sit repellendus qui ut at
                          blanditiis et quo et molestiae.
                        </p>
                      </div>
                      <span class="pointer-events-none absolute top-6 right-6 text-gray-300 group-hover:text-gray-400"
                            aria-hidden="true">
          <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 24 24">
            <path
              d="M20 4h1a1 1 0 00-1-1v1zm-1 12a1 1 0 102 0h-2zM8 3a1 1 0 000 2V3zM3.293 19.293a1 1 0 101.414 1.414l-1.414-1.414zM19 4v12h2V4h-2zm1-1H8v2h12V3zm-.707.293l-16 16 1.414 1.414 16-16-1.414-1.414z"/>
          </svg>
        </span>
                    </div>

                    <div
                      class="    relative group bg-white p-6 focus-within:ring-2 focus-within:ring-inset focus-within:ring-indigo-500">
                      <div>
          <span class="rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white">
            <svg class="h-6 w-6" x-description="Heroicon name: outline/users" xmlns="http://www.w3.org/2000/svg"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
        d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/>
</svg>
          </span>
                      </div>
                      <div class="mt-8">
                        <h3 class="text-lg font-medium">
                          <a href="#" class="focus:outline-none">

                            <span class="absolute inset-0" aria-hidden="true"/>
                            Schedule a one-on-one
                          </a>
                        </h3>
                        <p class="mt-2 text-sm text-gray-500">
                          Doloribus dolores nostrum quia qui natus officia quod et dolorem. Sit repellendus qui ut at
                          blanditiis et quo et molestiae.
                        </p>
                      </div>
                      <span class="pointer-events-none absolute top-6 right-6 text-gray-300 group-hover:text-gray-400"
                            aria-hidden="true">
          <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 24 24">
            <path
              d="M20 4h1a1 1 0 00-1-1v1zm-1 12a1 1 0 102 0h-2zM8 3a1 1 0 000 2V3zM3.293 19.293a1 1 0 101.414 1.414l-1.414-1.414zM19 4v12h2V4h-2zm1-1H8v2h12V3zm-.707.293l-16 16 1.414 1.414 16-16-1.414-1.414z"/>
          </svg>
        </span>
                    </div>

                    <div
                      class="    relative group bg-white p-6 focus-within:ring-2 focus-within:ring-inset focus-within:ring-indigo-500">
                      <div>
          <span class="rounded-lg inline-flex p-3 bg-yellow-50 text-yellow-700 ring-4 ring-white">
            <svg class="h-6 w-6" x-description="Heroicon name: outline/cash" xmlns="http://www.w3.org/2000/svg"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
        d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"/>
</svg>
          </span>
                      </div>
                      <div class="mt-8">
                        <h3 class="text-lg font-medium">
                          <a href="#" class="focus:outline-none">
                            <span class="absolute inset-0" aria-hidden="true"/>
                            Payroll
                          </a>
                        </h3>
                        <p class="mt-2 text-sm text-gray-500">
                          Doloribus dolores nostrum quia qui natus officia quod et dolorem. Sit repellendus qui ut at
                          blanditiis et quo et molestiae.
                        </p>
                      </div>
                      <span class="pointer-events-none absolute top-6 right-6 text-gray-300 group-hover:text-gray-400"
                            aria-hidden="true">
          <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 24 24">
            <path
              d="M20 4h1a1 1 0 00-1-1v1zm-1 12a1 1 0 102 0h-2zM8 3a1 1 0 000 2V3zM3.293 19.293a1 1 0 101.414 1.414l-1.414-1.414zM19 4v12h2V4h-2zm1-1H8v2h12V3zm-.707.293l-16 16 1.414 1.414 16-16-1.414-1.414z"/>
          </svg>
        </span>
                    </div>

                    <div
                      class="  sm:rounded-bl-lg  relative group bg-white p-6 focus-within:ring-2 focus-within:ring-inset focus-within:ring-indigo-500">
                      <div>
          <span class="rounded-lg inline-flex p-3 bg-rose-50 text-rose-700 ring-4 ring-white">
            <svg class="h-6 w-6" x-description="Heroicon name: outline/receipt-refund"
                 xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"
                 aria-hidden="true">
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
        d="M16 15v-1a4 4 0 00-4-4H8m0 0l3 3m-3-3l3-3m9 14V5a2 2 0 00-2-2H6a2 2 0 00-2 2v16l4-2 4 2 4-2 4 2z"/>
</svg>
          </span>
                      </div>
                      <div class="mt-8">
                        <h3 class="text-lg font-medium">
                          <a href="#" class="focus:outline-none">
                            <span class="absolute inset-0" aria-hidden="true"/>
                            Submit an expense
                          </a>
                        </h3>
                        <p class="mt-2 text-sm text-gray-500">
                          Doloribus dolores nostrum quia qui natus officia quod et dolorem. Sit repellendus qui ut at
                          blanditiis et quo et molestiae.
                        </p>
                      </div>
                      <span class="pointer-events-none absolute top-6 right-6 text-gray-300 group-hover:text-gray-400"
                            aria-hidden="true">
          <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 24 24">
            <path
              d="M20 4h1a1 1 0 00-1-1v1zm-1 12a1 1 0 102 0h-2zM8 3a1 1 0 000 2V3zM3.293 19.293a1 1 0 101.414 1.414l-1.414-1.414zM19 4v12h2V4h-2zm1-1H8v2h12V3zm-.707.293l-16 16 1.414 1.414 16-16-1.414-1.414z"/>
          </svg>
        </span>
                    </div>

                    <div
                      class="   rounded-bl-lg rounded-br-lg sm:rounded-bl-none relative group bg-white p-6 focus-within:ring-2 focus-within:ring-inset focus-within:ring-indigo-500">
                      <div>
          <span class="rounded-lg inline-flex p-3 bg-indigo-50 text-indigo-700 ring-4 ring-white">
            <svg class="h-6 w-6" x-description="Heroicon name: outline/academic-cap" xmlns="http://www.w3.org/2000/svg"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
  <path fill="#fff" d="M12 14l9-5-9-5-9 5 9 5z"/>
  <path fill="#fff"
        d="M12 14l6.16-3.422a12.083 12.083 0 01.665 6.479A11.952 11.952 0 0012 20.055a11.952 11.952 0 00-6.824-2.998 12.078 12.078 0 01.665-6.479L12 14z"/>
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
        d="M12 14l9-5-9-5-9 5 9 5zm0 0l6.16-3.422a12.083 12.083 0 01.665 6.479A11.952 11.952 0 0012 20.055a11.952 11.952 0 00-6.824-2.998 12.078 12.078 0 01.665-6.479L12 14zm-4 6v-7.5l4-2.222"/>
</svg>
          </span>
                      </div>
                      <div class="mt-8">
                        <h3 class="text-lg font-medium">
                          <a href="#" class="focus:outline-none">

                            <span class="absolute inset-0" aria-hidden="true"/>
                            Training
                          </a>
                        </h3>
                        <p class="mt-2 text-sm text-gray-500">
                          Doloribus dolores nostrum quia qui natus officia quod et dolorem. Sit repellendus qui ut at
                          blanditiis et quo et molestiae.
                        </p>
                      </div>
                      <span class="pointer-events-none absolute top-6 right-6 text-gray-300 group-hover:text-gray-400"
                            aria-hidden="true">
          <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 24 24">
            <path
              d="M20 4h1a1 1 0 00-1-1v1zm-1 12a1 1 0 102 0h-2zM8 3a1 1 0 000 2V3zM3.293 19.293a1 1 0 101.414 1.414l-1.414-1.414zM19 4v12h2V4h-2zm1-1H8v2h12V3zm-.707.293l-16 16 1.414 1.414 16-16-1.414-1.414z"/>
          </svg>
        </span>
                    </div>

                  </div>

                </div>
              </div>
            </div>
          </div>
          <div slot="buttons">

            <button type="button"
                    class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
              Cancel
            </button>
            <button type="button"
                    class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-red-600 text-base font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 sm:ml-3 sm:w-auto sm:text-sm">
              Deactivate
            </button>
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }
}
