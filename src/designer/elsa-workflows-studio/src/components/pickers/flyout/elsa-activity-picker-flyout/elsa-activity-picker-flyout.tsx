import {Component, Host, h} from '@stencil/core';

@Component({
  tag: 'elsa-activity-picker-flyout',
  styleUrl: 'elsa-activity-picker-flyout.css',
  shadow: false,
})
export class ElsaActivityPickerFlyout {

  render() {
    return (
      <Host>
        <div class="absolute inset-0 overflow-hidden">
          <section class="absolute inset-y-0 right-0 pl-10 max-w-md md:max-w-full flex sm:pl-16">
            <div class="w-screen max-w-2xl"
                 data-transition-enter="transform transition ease-in-out duration-500 sm:duration-700"
                 data-transition-enter-start="translate-x-full"
                 data-transition-enter-end="translate-x-0"
                 data-transition-leave="transform transition ease-in-out duration-500 sm:duration-700"
                 data-transition-leave-start="translate-x-0"
                 data-transition-leave-end="translate-x-full">
              <div class="h-full flex flex-col bg-white shadow-xl overflow-y-scroll">
                <div class="flex-1">
                  <header class="p-6">
                    <div class="flex items-start justify-between space-x-3">
                        <h2 class="text-lg leading-7 font-medium text-gray-900">
                          Title
                        </h2>
                      <div class="h-7 flex items-center">
                        <button aria-label="Close panel"
                                class="text-gray-400 hover:text-gray-500 transition ease-in-out duration-150">
                          <svg class="h-6 w-6" x-description="Heroicon name: x" xmlns="http://www.w3.org/2000/svg"
                               fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                  d="M6 18L18 6M6 6l12 12"/>
                          </svg>
                        </button>
                      </div>
                    </div>
                  </header>
                  <slot>Content goes here</slot>
                </div>
                <div class="flex-shrink-0 px-4 py-4 space-x-4 flex justify-end">
                        <span class="inline-flex rounded-md shadow-sm">
                        <button type="button"
                                class="inline-flex justify-center py-2 px-4 border rounded-md text-sm leading-5 font-medium focus:outline-none focus:shadow-outline-blue transition duration-150 ease-in-out border-transparent text-white bg-blue-600 hover:bg-blue-500 focus:border-blue-700 active:bg-blue-700">
                          Submit
                        </button>
                        </span>
                </div>
              </div>
            </div>
          </section>
        </div>
      </Host>
    );
  }
}
