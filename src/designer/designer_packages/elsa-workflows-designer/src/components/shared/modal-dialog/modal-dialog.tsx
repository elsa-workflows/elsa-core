import {Component, Host, h, State, Listen, Method, Event, Element, EventEmitter, Prop} from '@stencil/core';
import {enter, leave} from 'el-transition'
import {ModalActionClickArgs, ModalActionDefinition, ModalActionType, ModalDialogInstance} from "./models";
import {ModalType} from "./modal-type";

@Component({
  tag: 'elsa-modal-dialog',
  shadow: false,
})
export class ModalDialog {
  private overlay: HTMLElement
  private modal: HTMLElement

  @Prop() modalDialogInstance: ModalDialogInstance;
  @Prop() actions: Array<ModalActionDefinition> = [];
  @Prop() size: string = 'max-w-6xl';
  @Prop() type: ModalType = ModalType.Default;
  @Prop() autoHide: boolean = true;
  @Prop() content: () => any = () => <div/>;
  @Event() shown: EventEmitter;
  @Event() hidden: EventEmitter;
  @Event() actionInvoked: EventEmitter<ModalActionClickArgs>;
  @State() private isVisible: boolean = true;
  @Element() element;

  @Method()
  async show(animate: boolean = true) {
    this.showInternal(animate);
  }

  @Method()
  async hide(animate: boolean = true) {
    this.hideInternal(animate);
  }

  handleDefaultClose = async () => {
    await this.hide();
  }

  showInternal(animate: boolean) {
    this.isVisible = true;

    if (!animate) {
      this.overlay.style.opacity = '1';
      this.modal.style.opacity = '1';
    }

    enter(this.overlay);
    enter(this.modal).then(this.shown.emit);
  }

  hideInternal(animate: boolean) {
    if (!animate) {
      this.isVisible = false
    }

    leave(this.overlay);
    leave(this.modal).then(() => {
      this.isVisible = false;
      this.hidden.emit();
    });
  }

  @Listen('keydown', {target: 'window'})
  async handleKeyDown(e: KeyboardEvent) {
    if (this.isVisible && e.key === 'Escape') {
      await this.hide(true);
    }
  }

  componentDidRender() {
    if (this.isVisible) {
      enter(this.overlay);
      enter(this.modal).then(this.shown.emit);
    }

    this.modalDialogInstance.modalDialogContentRef = this.element.querySelector('.modal-content').children[0];
  }

  render() {
    const actions = this.actions;
    const content = this.content();

    return (
      <Host class={{'hidden': !this.isVisible, 'block': true}}>
        <div class="fixed z-50 inset-0 overflow-y-auto">
          <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
            <div ref={el => this.overlay = el}
                 onClick={() => this.hide(true)}
                 data-transition-enter="ease-out duration-300" data-transition-enter-start="opacity-0"
                 data-transition-enter-end="opacity-0" data-transition-leave="ease-in duration-200"
                 data-transition-leave-start="opacity-0" data-transition-leave-end="opacity-0"
                 class="hidden fixed inset-0 transition-opacity" aria-hidden="true">
              <div class="absolute inset-0 bg-gray-500 opacity-75"/>
            </div>

            <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true"/>
            <div ref={el => this.modal = el}
                 data-transition-enter="ease-out duration-300"
                 data-transition-enter-start="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                 data-transition-enter-end="opacity-0 translate-y-0 sm:scale-100"
                 data-transition-leave="ease-in duration-200"
                 data-transition-leave-start="opacity-0 translate-y-0 sm:scale-100"
                 data-transition-leave-end="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                 class={`hidden inline-block sm:align-top bg-white rounded-lg text-left overflow-visible shadow-xl transform transition-all sm:my-8 sm:align-top ${this.size}`}
                 role="dialog" aria-modal="true" aria-labelledby="modal-headline">
              <div class="modal-content">
                {content}
              </div>

              <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">

                {actions.map(action => {

                  if (action.display)
                    return action.display(action);

                  const cssClass = action.isPrimary
                    ? 'text-white bg-blue-600 hover:bg-blue-700 border-transparent focus:ring-blue-500'
                    : action.isDangerous ? 'text-white bg-red-600 hover:bg-red-700 border-transparent focus:ring-red-500'
                      : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50 focus:ring-blue-500';

                  const buttonType = action.type == ModalActionType.Submit ? 'submit' : 'button';
                  const cancelHandler = () => this.hideInternal(true);
                  const defaultHandler = (args: ModalActionClickArgs) => this.actionInvoked.emit(args);
                  const clickHandler = !!action.onClick ? action.onClick : action.type == ModalActionType.Cancel ? cancelHandler : defaultHandler;

                  return <button type={buttonType}
                                 onClick={e => {
                                   clickHandler({e, action, instance: this.modalDialogInstance});
                                   if (this.autoHide)
                                     this.hideInternal(true);
                                 }}
                                 class={`${cssClass} mt-3 w-full inline-flex justify-center rounded-md border shadow-sm px-4 py-2 text-base font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm`}>
                    {action.text}
                  </button>
                })}
              </div>
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
