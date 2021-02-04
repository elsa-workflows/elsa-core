import {Component, Host, h, Prop} from '@stencil/core';
import {eventBus} from '../../../../utils/event-bus';

@Component({
  tag: 'elsa-activity-picker-modal',
  styleUrl: 'elsa-activity-picker-modal.css',
  shadow: false,
})
export class ElsaActivityPickerModal {

  @Prop() show: boolean

  componentDidLoad(){
    eventBus.on('add-activity', () => console.debug('This is truly awesome :)'));
  }

  render() {
    return this.show ? this.renderModal() : undefined;
  }

  renderModal() {
    return (
      <Host>
        <div class="fixed z-10 inset-0 overflow-y-auto">
          <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
            <div x-show="open"
                 data-transition-enter="ease-out duration-300" data-transition-enter-start="opacity-0"
                 data-transition-enter-end="opacity-100" data-transition-leave="ease-in duration-200"
                 data-transition-leave-start="opacity-100" data-transition-leave-end="opacity-0"
                 class="fixed inset-0 transition-opacity" aria-hidden="true">
              <div class="absolute inset-0 bg-gray-500 opacity-75"/>
            </div>

            <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true"/>
            <div x-show="open"
                 data-transition-enter="ease-out duration-300"
                 data-transition-enter-start="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                 data-transition-enter-end="opacity-100 translate-y-0 sm:scale-100"
                 data-transition-leave="ease-in duration-200"
                 data-transition-leave-start="opacity-100 translate-y-0 sm:scale-100"
                 data-transition-leave-end="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                 class="inline-block align-bottom bg-white rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-sm sm:w-full sm:p-6"
                 role="dialog" aria-modal="true" aria-labelledby="modal-headline">
              <div>
                <div class="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100">
                  <svg class="h-6 w-6 text-green-600" x-description="Heroicon name: outline/check"
                       xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"
                       aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                  </svg>
                </div>
                <div class="mt-3 text-center sm:mt-5">
                  <h3 class="text-lg leading-6 font-medium text-gray-900" id="modal-headline">
                    Payment successful
                  </h3>
                  <div class="mt-2">
                    <p class="text-sm text-gray-500">
                      Lorem ipsum dolor sit amet consectetur adipisicing elit. Consequatur amet labore.
                    </p>
                  </div>
                </div>
              </div>
              <div class="mt-5 sm:mt-6">
                <button
                  type="button"
                  class="inline-flex justify-center w-full rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:text-sm">
                  Go back to dashboard
                </button>
              </div>
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
