import {Component, Host, h, Prop, State, Listen, Method} from '@stencil/core';
import {enter, leave, toggle} from 'el-transition'

@Component({
  tag: 'elsa-modal-dialog',
  styleUrl: 'elsa-modal-dialog.css',
  shadow: false,
})
export class ElsaModalDialog {

  @State() isVisible: boolean;
  overlay: HTMLElement
  modal: HTMLElement

  render() {
    return this.renderModal();
  }

  @Method()
  async show(animate: boolean) {
    this.showInternal(animate);
  }

  @Method()
  async hide(animate: boolean) {
    this.hideInternal(animate);
  }

  showInternal(animate: boolean) {
    this.isVisible = true;

    if (!animate) {
      this.overlay.style.opacity = "1";
      this.modal.style.opacity = "1";
    }

    enter(this.overlay);
    enter(this.modal);
  }

  hideInternal(animate: boolean) {
    if (!animate) {
      this.isVisible = false
    }

    leave(this.overlay);
    leave(this.modal).then(() => this.isVisible = false);
  }

  renderModal() {
    return (
      <Host class={{hidden: !this.isVisible}}>
        <div class="fixed z-10 inset-0 overflow-y-auto">
          <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
            <div ref={el => this.overlay = el}
                 onClick={() => this.hide(true)}
                 data-transition-enter="ease-out duration-300" data-transition-enter-start="opacity-0"
                 data-transition-enter-end="opacity-100" data-transition-leave="ease-in duration-200"
                 data-transition-leave-start="opacity-100" data-transition-leave-end="opacity-0"
                 class="hidden fixed inset-0 transition-opacity" aria-hidden="true">
              <div class="absolute inset-0 bg-gray-500 opacity-75"/>
            </div>

            <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true"/>
            <div ref={el => this.modal = el}
                 data-transition-enter="ease-out duration-300"
                 data-transition-enter-start="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                 data-transition-enter-end="opacity-100 translate-y-0 sm:scale-100"
                 data-transition-leave="ease-in duration-200"
                 data-transition-leave-start="opacity-100 translate-y-0 sm:scale-100"
                 data-transition-leave-end="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                 class="hidden inline-block sm:align-top bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-top sm:max-w-4xl sm:w-full"
                 role="dialog" aria-modal="true" aria-labelledby="modal-headline">
              <div class="modal-content">
                <slot name="content"/>
              </div>

              <slot name="buttons">
                <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
                  <button type="button"
                          class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
                    Close
                  </button>
                </div>
              </slot>
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
