import {Component, Event, EventEmitter, h, Prop} from '@stencil/core';
import {TabChangedArgs, TabDefinition} from '../../../models';
import {isNullOrWhitespace} from "../../../utils";
import {PanelActionClickArgs, PanelActionDefinition, PanelActionType} from "./models";

@Component({
  tag: 'elsa-form-panel'
})
export class FormPanel {
  @Prop() public mainTitle: string;
  @Prop() public subTitle: string;
  @Prop() public isReadonly: boolean;
  @Prop() public orientation: 'Landscape' | 'Portrait' = 'Portrait';
  @Prop() public tabs: Array<TabDefinition> = [];
  @Prop({mutable: true}) public selectedTabIndex?: number;
  @Prop() public actions: Array<PanelActionDefinition> = [];

  @Event() public submitted: EventEmitter<FormData>;
  @Event() public selectedTabIndexChanged: EventEmitter<TabChangedArgs>;
  @Event() public actionInvoked: EventEmitter<PanelActionClickArgs>;

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
    const tabs = this.tabs.sort((a, b) => a.order < b.order ? -1 : a.order > b.order ? 1 : 0);
    const selectedTabIndex = this.selectedTabIndex;
    const actions = this.actions;
    const mainTitle = this.mainTitle;
    const subTitle = this.subTitle;
    const orientation = this.orientation;
    const readonly = this.isReadonly;

    return (
      <div class="tw-absolute tw-inset-0 tw-overflow-hidden">
        <form class="tw-h-full tw-flex tw-flex-col tw-bg-white tw-shadow-xl" onSubmit={e => this.onSubmit(e)} method="post">
        
          <div class="tw-flex tw-flex-col tw-flex-1">

            {orientation == 'Portrait' && (
              <div class="tw-px-4 tw-py-6 tw-bg-gray-50">

                <div class="tw-flex tw-items-start tw-justify-between tw-space-x-3">
                  <div class="tw-space-y-1">
                    <h2 class="tw-text-lg tw-font-medium tw-text-gray-900">
                      {mainTitle}
                    </h2>
                    {!isNullOrWhitespace(subTitle) ? <h3 class="tw-text-sm tw-text-gray-700">{subTitle}</h3> : undefined}
                  </div>
                </div>
              </div>)}

            {orientation == 'Landscape' && (
              <div class="tw-px-10 tw-py-4 tw-bg-gray-50">
                <div class="tw-flex tw-items-start tw-justify-between tw-space-x-3">
                  <div class="tw-space-y-0">
                    <h2 class="tw-text-base tw-font-medium tw-text-gray-900">
                      {mainTitle}
                    </h2>
                    {!isNullOrWhitespace(subTitle) ? <h3 class="tw-text-xs tw-text-gray-700">{subTitle}</h3> : undefined}
                  </div>
                </div>
              </div>)}

            <div class={`tw-border-b tw-border-gray-200 ${orientation == 'Landscape' ? 'tw-pl-10' : 'tw-pl-4'}`}>
              <nav class="-tw-mb-px tw-flex tw-justify-start tw-space-x-5" aria-label="Tabs">
                {tabs.map((tab, tabIndex) => {
                  const cssClass = tabIndex == selectedTabIndex ? 'tw-border-blue-500 tw-text-blue-600' : 'tw-border-transparent tw-text-gray-500 hover:tw-text-gray-700 hover:tw-border-gray-300';
                  return <a href="#"
                            onClick={e => this.onTabClick(e, tab)}
                            class={`${cssClass} tw-py-4 tw-px-1 tw-text-center tw-border-b-2 tw-font-medium tw-text-sm`}>
                    {tab.displayText}
                  </a>
                })}
              </nav>
            </div>

            <div class={`tw-flex-1 tw-relative`}>
              <div class={`tw-absolute tw-inset-0 tw-overflow-y-scroll ${orientation == 'Landscape' ? 'tw-px-6' : ''}`}>
               
                {tabs.map((tab, tabIndex) => {
                  const cssClass = tabIndex == selectedTabIndex ? '' : 'hidden';
                  return <div class={cssClass}>
                    <fieldset disabled={readonly}>
                      {tab.content()}
                    </fieldset>
                  </div>
                })}
               
              </div>
            </div>
          </div>

          {actions.length > 0 ? (
            <div class="tw-flex-shrink-0 tw-px-4 tw-border-t tw-border-gray-200 tw-py-5 sm:tw-px-6">
              <div class="tw-space-x-3 tw-flex tw-justify-end">
                {actions.map(action => {

                  if (action.display)
                    return action.display(action);

                  const cssClass = action.isPrimary
                    ? 'tw-text-white tw-bg-blue-600 hover:tw-bg-blue-700 tw-border-transparent focus:tw-ring-blue-500'
                    : action.isDangerous ? 'tw-text-white tw-bg-red-600 hover:tw-bg-red-700 tw-border-transparent focus:tw-ring-red-500'
                      : 'tw-bg-white tw-border-gray-300 tw-text-gray-700 hover:tw-bg-gray-50 focus:tw-ring-blue-500';

                  const buttonType = action.type == PanelActionType.Submit ? 'submit' : 'button';

                  const cancelHandler = () => {
                  };

                  const defaultHandler = (e: any, action: PanelActionDefinition) => this.actionInvoked.emit({e, action: action});
                  const clickHandler = !!action.onClick ? action.onClick : action.type == PanelActionType.Cancel ? cancelHandler : defaultHandler;

                  return <button type={buttonType}
                                 onClick={e => clickHandler(e, action)}
                                 class={`${cssClass} tw-py-2 tw-px-4 tw-border tw-rounded-md tw-shadow-sm tw-text-sm tw-font-medium focus:tw-outline-none focus:tw-ring-2 focus:tw-ring-offset-2`}>
                    {action.text}
                  </button>
                })}
              </div>
              </div>) : undefined}
        
        </form>
      </div>
    );
  }
}
