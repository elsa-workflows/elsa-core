import {Component, Host, h, Prop, State, Listen, Method} from '@stencil/core';
import {enter, leave, toggle} from 'el-transition'

export interface ToastNotificationOptions {
  autoCloseIn?: number;
  title?: string;
  message: string;
}

@Component({
  tag: 'elsa-toast-notification',
  styleUrl: 'elsa-toast-notification.css',
  shadow: false,
})
export class ElsaToastNotification {

  @State() isVisible: boolean = false;
  @State() title?: string;
  @State() message?: string;
  toast: HTMLElement;

  @Method()
  async show(options: ToastNotificationOptions) {
    this.isVisible = true;
    enter(this.toast);

    if (options.autoCloseIn)
      setTimeout(async () => await this.hide(), options.autoCloseIn);

    this.title = options.title;
    this.message = options.message;
  }

  @Method()
  async hide() {
    leave(this.toast).then(() => this.isVisible = false);
  }

  render() {
    return this.renderToast();
  }

  renderToast() {
    return (
      <Host class={{hidden: !this.isVisible}}>
        <div class="fixed inset-0 flex items-end justify-center px-4 py-6 pointer-events-none sm:p-6 sm:items-start sm:justify-end">
          <div ref={el => this.toast = el}
               data-transition-enter="transform ease-out duration-300 transition"
               data-transition-enter-start="translate-y-2 opacity-0 sm:translate-y-0 sm:translate-x-2"
               data-transition-enter-end="translate-y-0 opacity-100 sm:translate-x-0"
               data-transition-leave="transition ease-in duration-100"
               data-transition-leave-start="opacity-100"
               data-transition-leave-end="opacity-0"
               class="max-w-sm w-full bg-white shadow-lg rounded-lg pointer-events-auto ring-1 ring-black ring-opacity-5 overflow-hidden">
            <div class="p-4">
              <div class="flex items-start">
                <div class="flex-shrink-0">
                  <svg class="h-6 w-6 text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                  </svg>
                </div>
                <div class="ml-3 w-0 flex-1 pt-0.5">
                  {this.renderTitle()}
                  <p class="mt-1 text-sm text-gray-500">
                    {this.message}
                  </p>
                </div>
                <div class="ml-4 flex-shrink-0 flex">
                  <button class="bg-white rounded-md inline-flex text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                    <span class="sr-only">Close</span>
                    <svg class="h-5 w-5" x-description="Heroicon name: solid/x" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                      <path fill-rule="evenodd"
                            d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                            clip-rule="evenodd"/>
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Host>
    );
  }

  renderTitle() {
    if (!this.title || this.title.length == 0)
      return undefined;

    return (
      <p class="text-sm font-medium text-gray-900">
        {this.title}
      </p>
    );
  }
}
