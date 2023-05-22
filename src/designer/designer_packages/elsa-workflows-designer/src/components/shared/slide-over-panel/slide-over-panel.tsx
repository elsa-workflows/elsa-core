import {Component, Event, EventEmitter, h, Method, Prop, State, Watch} from '@stencil/core';
import {TabDefinition} from '../../../models';
import {PanelActionDefinition, PanelActionType} from "../../shared/form-panel/models";

@Component({
  tag: 'elsa-slide-over-panel'
})
export class SlideOverPanel {
  private overlayElement: HTMLElement;
  private formElement: HTMLFormElement;

  @Prop() public headerText: string;
  @Prop() public tabs: Array<TabDefinition> = [];
  @Prop({mutable: true}) public selectedTab?: TabDefinition;
  @Prop() public actions: Array<PanelActionDefinition> = [];
  @Prop() public expand: boolean;
  @Event() public collapsed: EventEmitter;

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

  private renderPanel() {
    const isVisible = this.isVisible;
    const isHiding = this.isHiding;
    const wrapperClass = isVisible ? 'tw-block' : 'hidden';
    const backdropClass = !isHiding && isVisible ? 'tw-opacity-50' : 'tw-opacity-0';
    const panelClass = !isHiding && isVisible ? 'tw-max-w-2xl w-2xl' : 'tw-max-tw-w-0 tw-w-0';
    const tabs = this.tabs;
    const selectedTab = this.selectedTab;
    const actions = this.actions;

    return (
      <div class={`tw-fixed tw-inset-0 tw-overflow-hidden tw-z-10 ${wrapperClass}`} aria-labelledby="slide-over-title" role="dialog"
           aria-modal="true">
        <div class="tw-absolute tw-inset-0 tw-overflow-hidden">

          <div class={`tw-absolute tw-inset-0 tw-bg-gray-100 tw-ease-in-out tw-duration-200 ${backdropClass}`}
               onTransitionEnd={e => this.onTransitionEnd(e)}/>

          <div class="tw-absolute tw-inset-0" aria-hidden="true" onClick={e => this.onOverlayClick(e)}
               ref={el => this.overlayElement = el}>

            <div class="tw-fixed tw-inset-y-0 tw-right-0 tw-pl-10 tw-max-w-full tw-flex sm:tw-pl-16">

              <div class={`tw-w-screen tw-ease-in-out tw-duration-200 ${panelClass}`}>
                <form class="tw-h-full tw-flex tw-flex-col tw-bg-white tw-shadow-xl"
                      ref={el => this.formElement = el} method="post">
                  <div class="tw-flex tw-flex-col tw-flex-1">

                    <div class="tw-px-4 tw-py-6 tw-bg-gray-50 sm:tw-px-6">
                      <div class="tw-flex tw-items-start tw-justify-between tw-space-x-3">
                        <div class="tw-space-y-1">
                          <h2 class="tw-text-lg tw-font-medium tw-text-gray-900" id="slide-over-title">
                            {this.headerText}
                          </h2>
                        </div>
                        <div class="tw-h-7 tw-flex tw-items-center">
                          <button type="button" class="tw-text-gray-400 hover:tw-text-gray-500"
                                  onClick={() => this.onCloseClick()}>
                            <span class="tw-sr-only">Close panel</span>
                            <svg class="tw-h-6 tw-w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"
                                 stroke="currentColor" aria-hidden="true">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                    d="M6 18L18 6M6 6l12 12"/>
                            </svg>
                          </button>
                        </div>
                      </div>
                    </div>

                    <div class="tw-border-b tw-border-gray-200">
                      <nav class="-tw-mb-px tw-flex" aria-label="Tabs">
                        {tabs.map(tab => {
                          const cssClass = tab == selectedTab ? 'tw-border-blue-500 tw-text-blue-600' : 'tw-border-transparent tw-text-gray-500 hover:tw-text-gray-700 hover:tw-border-gray-300';
                          return <a href="#"
                                    onClick={e => this.onTabClick(e, tab)}
                                    class={`${cssClass} tw-py-4 tw-px-1 tw-text-center tw-border-b-2 tw-font-medium tw-text-sm tw-flex-1`}>
                            {tab.displayText}
                          </a>
                        })}
                      </nav>
                    </div>

                    <div class="tw-flex-1 tw-relative">

                      <div class="tw-absolute tw-inset-0 tw-overflow-y-scroll">
                        {tabs.map(tab => {
                          const cssClass = tab == selectedTab ? '' : 'hidden';
                          return <div class={cssClass}>
                            {tab.content()}
                          </div>
                        })}
                      </div>
                    </div>
                  </div>

                  <div class="tw-flex-shrink-0 tw-px-4 tw-border-t tw-border-gray-200 tw-py-5 sm:tw-px-6">
                    <div class="tw-space-x-3 tw-flex tw-justify-end">
                      {actions.map(action => {

                        if (action.display)
                          return action.display(action);

                        const cssClass = action.isPrimary ? 'tw-text-white tw-bg-blue-600 hover:tw-bg-blue-700 tw-border-transparent' : 'tw-bg-white tw-border-gray-300 tw-text-gray-700 hover:tw-bg-gray-50';
                        const buttonType = action.type == PanelActionType.Submit ? 'submit' : 'button';
                        const cancelHandler = async () => await this.hide();

                        const emptyHandler = () => {
                        };

                        const clickHandler = !!action.onClick ? action.onClick : action.type == PanelActionType.Cancel ? cancelHandler : emptyHandler;

                        return <button type={buttonType}
                                       onClick={e => clickHandler({e, action})}
                                       class={`${cssClass} tw-py-2 tw-px-4 tw-border tw-rounded-md tw-shadow-sm tw-text-sm tw-font-medium focus:tw-outline-none focus:tw-ring-2 focus:tw-ring-offset-2 focus:tw-ring-blue-500`}>
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
