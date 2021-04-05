import {Component, h, Host, Method, State} from '@stencil/core';

@Component({
  tag: 'elsa-confirm-dialog',
  shadow: false,
})
export class ElsaConfirmDialog {

  @State() caption: string;
  @State() message: string;

  dialog: HTMLElsaModalDialogElement;
  fulFill: (value: (PromiseLike<boolean> | boolean)) => void;
  reject: () => void;

  @Method()
  async show(caption: string, message: string): Promise<boolean> {
    this.caption = caption;
    this.message = message;

    await this.dialog.show(true);

    return new Promise<boolean>((fulfill, reject) => {
      this.fulFill = fulfill;
      this.reject = reject;
    });
  }

  @Method()
  async hide() {
    await this.dialog.hide(true);
  }

  async onDismissClick() {
    this.fulFill(false);
    await this.hide();
  }

  async onAcceptClick() {
    this.fulFill(true);
    this.fulFill = null;
    this.reject = null;
    await this.hide();
  }

  render() {
    return (
      <Host>
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="py-8 px-4">
            <div class="hidden sm:block absolute top-0 right-0 pt-4 pr-4">
              <button type="button"
                      onClick={() => this.onDismissClick()}
                      class="bg-white rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                <span class="sr-only">Close</span>
                <svg class="h-6 w-6" x-description="Heroicon name: outline/x" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>
            <div class="sm:flex sm:items-start">
              <div class="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-red-100 sm:mx-0 sm:h-10 sm:w-10">
                <svg class="h-6 w-6 text-red-600" x-description="Heroicon name: outline/exclamation" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                </svg>
              </div>
              <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
                <h3 class="text-lg leading-6 font-medium text-gray-900" id="modal-title">
                  {this.caption}
                </h3>
                <div class="mt-2">
                  <p class="text-sm text-gray-500">
                    {this.message}
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div slot="buttons">
            <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button type="button"
                      onClick={() => this.onAcceptClick()}
                      class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-red-600 text-base font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 sm:ml-3 sm:w-auto sm:text-sm">
                Yes
              </button>
              <button type="button"
                      onClick={() => this.onDismissClick()}
                      class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:w-auto sm:text-sm">
                No
              </button>
            </div>
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }
}
