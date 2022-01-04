import {Component, Event, EventEmitter, h, Method, Prop, State, Watch} from '@stencil/core';
import {ActionDefinition, ActionType, TabDefinition} from '../../../models';

@Component({
  tag: 'elsa-slide-over-panel'
})
export class SlideOverPanel {
  private overlayElement: HTMLElement;
  private formElement: HTMLFormElement;

  @Prop() public headerText: string;
  @Prop() public tabs: Array<TabDefinition> = [];
  @Prop({mutable: true}) public selectedTab?: TabDefinition;
  @Prop() public actions: Array<ActionDefinition> = [];
  @Prop() public expand: boolean;

  @Event() public collapsed: EventEmitter;
  @Event() public submitted: EventEmitter<FormData>;


  @Method()
  public async show(): Promise<void> {
    this.isShowing = true;
    this.isHiding = false;
    this.isVisible = true;
  }

  @Method()
  public async hide(): Promise<void> {
    this.isHiding = true;
    this.isShowing = false;
  }

  @State() public isHiding: boolean = false;
  @State() public isShowing: boolean = false;
  @State() public isVisible: boolean = false;

  @Watch('expand')
  private handleExpanded(value: boolean) {
    this.isShowing = value;
    this.isHiding = !value;

    if (value)
      this.isVisible = true;
  }

  public render() {
    return this.renderPanel();
  }

  private onCloseClick = async () => {
    await this.hide();
  };

  private onOverlayClick = async (e: MouseEvent) => {
    if (e.target != this.overlayElement)
      return;

    // Hide panel.
    await this.hide();

    // Raise Form Submitted event to apply changes.
    const formData = new FormData(this.formElement);

    this.submitted.emit(formData);
  };

  private onTransitionEnd = (e: TransitionEvent) => {
    if (this.isHiding) {
      this.isVisible = false;
      this.isHiding = false;
      this.collapsed.emit();
    }
  };

  private onTabClick(e: Event, tab: TabDefinition) {
    e.preventDefault();
    this.selectedTab = tab;
  }

  private onSubmit(e: Event) {
    e.preventDefault();
    const formData = new FormData(e.target as HTMLFormElement);
    this.submitted.emit(formData);
  }

  private renderPanel() {
    const isVisible = this.isVisible;
    const isHiding = this.isHiding;
    const wrapperClass = isVisible ? 'block' : 'hidden';
    const backdropClass = !isHiding && isVisible ? 'opacity-50' : 'opacity-0';
    const panelClass = !isHiding && isVisible ? 'max-w-2xl w-2xl' : 'max-w-0 w-0';
    const tabs = this.tabs;
    const selectedTab = this.selectedTab;
    const actions = this.actions;

    return (
      <div class={`fixed inset-0 overflow-hidden z-10 ${wrapperClass}`} aria-labelledby="slide-over-title" role="dialog"
           aria-modal="true">
        <div class="absolute inset-0 overflow-hidden">

          <div class={`absolute inset-0 bg-gray-100 ease-in-out duration-200 ${backdropClass}`}
               onTransitionEnd={e => this.onTransitionEnd(e)}/>

          <div class="absolute inset-0" aria-hidden="true" onClick={e => this.onOverlayClick(e)}
               ref={el => this.overlayElement = el}>

            <div class="fixed inset-y-0 right-0 pl-10 max-w-full flex sm:pl-16">

              <div class={`w-screen ease-in-out duration-200 ${panelClass}`}>
                <form class="h-full flex flex-col bg-white shadow-xl" onSubmit={e => this.onSubmit(e)}
                      ref={el => this.formElement = el} method="post">
                  <div class="flex flex-col flex-1">

                    <div class="px-4 py-6 bg-gray-50 sm:px-6">
                      <div class="flex items-start justify-between space-x-3">
                        <div class="space-y-1">
                          <h2 class="text-lg font-medium text-gray-900" id="slide-over-title">
                            {this.headerText}
                          </h2>
                        </div>
                        <div class="h-7 flex items-center">
                          <button type="button" class="text-gray-400 hover:text-gray-500"
                                  onClick={() => this.onCloseClick()}>
                            <span class="sr-only">Close panel</span>
                            <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"
                                 stroke="currentColor" aria-hidden="true">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                    d="M6 18L18 6M6 6l12 12"/>
                            </svg>
                          </button>
                        </div>
                      </div>
                    </div>

                    <div class="border-b border-gray-200">
                      <nav class="-mb-px flex" aria-label="Tabs">
                        {tabs.map(tab => {
                          const cssClass = tab == selectedTab ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300';
                          return <a href="#"
                                    onClick={e => this.onTabClick(e, tab)}
                                    class={`${cssClass} py-4 px-1 text-center border-b-2 font-medium text-sm flex-1`}>
                            {tab.displayText}
                          </a>
                        })}
                      </nav>
                    </div>

                    <div class="flex-1 relative">

                      <div class="absolute inset-0 overflow-y-scroll">
                        {tabs.map(tab => {
                          const cssClass = tab == selectedTab ? '' : 'hidden';
                          return <div class={cssClass}>
                            {tab.content()}
                          </div>
                        })}
                      </div>
                    </div>
                  </div>

                  <div class="flex-shrink-0 px-4 border-t border-gray-200 py-5 sm:px-6">
                    <div class="space-x-3 flex justify-end">
                      {actions.map(action => {

                        if (action.display)
                          return action.display(action);

                        const cssClass = action.isPrimary ? 'text-white bg-blue-600 hover:bg-blue-700 border-transparent' : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50';
                        const buttonType = action.type == ActionType.Submit ? 'submit' : 'button';
                        const cancelHandler = async () => await this.hide();

                        const emptyHandler = () => {
                        };

                        const clickHandler = !!action.onClick ? action.onClick : action.type == ActionType.Cancel ? cancelHandler : emptyHandler;

                        return <button type={buttonType}
                                       onClick={e => clickHandler(e, action)}
                                       class={`${cssClass} py-2 px-4 border rounded-md shadow-sm text-sm font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500`}>
                          {action.text}
                        </button>
                      })}
                    </div>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
