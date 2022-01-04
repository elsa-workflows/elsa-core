import {Component, Event, EventEmitter, h, Prop} from '@stencil/core';
import {ActionDefinition, ActionInvokedArgs, ActionType, TabChangedArgs, TabDefinition} from '../../../models';

@Component({
  tag: 'elsa-form-panel'
})
export class FormPanel {
  private formElement: HTMLFormElement;

  @Prop() public headerText: string;
  @Prop() public tabs: Array<TabDefinition> = [];
  @Prop({mutable: true}) public selectedTabIndex?: number;
  @Prop() public actions: Array<ActionDefinition> = [];

  @Event() public submitted: EventEmitter<FormData>;
  @Event() public selectedTabIndexChanged: EventEmitter<TabChangedArgs>;
  @Event() public actionInvoked: EventEmitter<ActionInvokedArgs>;

  public render() {
    return this.renderPanel();
  }

  private onTabClick(e: Event, tab: TabDefinition) {
    e.preventDefault();
    const selectedTabIndex = this.selectedTabIndex = this.tabs.findIndex(x => tab == x);
    this.selectedTabIndexChanged.emit({selectedTabIndex: selectedTabIndex});
  }

  private onSubmit(e: Event) {
    e.preventDefault();
    const formData = new FormData(e.target as HTMLFormElement);
    this.submitted.emit(formData);
  }

  private renderPanel() {
    const tabs = this.tabs;
    const selectedTabIndex = this.selectedTabIndex;
    const actions = this.actions;

    return (
      <div class="absolute inset-0 overflow-hidden">
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
              </div>
            </div>

            <div class="border-b border-gray-200">
              <nav class="-mb-px flex" aria-label="Tabs">
                {tabs.map((tab, tabIndex) => {
                  const cssClass = tabIndex == selectedTabIndex ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300';
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
                {tabs.map((tab, tabIndex) => {
                  const cssClass = tabIndex == selectedTabIndex ? '' : 'hidden';
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

                const cssClass = action.isPrimary
                  ? 'text-white bg-blue-600 hover:bg-blue-700 border-transparent focus:ring-blue-500'
                  : action.isDangerous ? 'text-white bg-red-600 hover:bg-red-700 border-transparent focus:ring-red-500'
                    : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50 focus:ring-blue-500';

                const buttonType = action.type == ActionType.Submit ? 'submit' : 'button';

                const cancelHandler = () => {
                };

                const defaultHandler = (e: any, action: ActionDefinition) => this.actionInvoked.emit({action: action});
                const clickHandler = !!action.onClick ? action.onClick : action.type == ActionType.Cancel ? cancelHandler : defaultHandler;

                return <button type={buttonType}
                               onClick={e => clickHandler(e, action)}
                               class={`${cssClass} py-2 px-4 border rounded-md shadow-sm text-sm font-medium focus:outline-none focus:ring-2 focus:ring-offset-2`}>
                  {action.text}
                </button>
              })}
            </div>
          </div>
        </form>
      </div>
    );
  }
}
