import {Component, h, Prop, State} from '@stencil/core';
import {Graph} from '@antv/x6';

@Component({
  tag: 'elsa-workflow-definition-editor-toolbox',
})
export class Toolbox {
  @Prop() graph: Graph;
  @Prop() isReadonly: boolean;
  @State() selectedTabIndex: number = 0;

  private onTabSelected = (e: Event, index: number) => {
    e.preventDefault();
    this.selectedTabIndex = index;
  };

  render() {

    const selectedTabIndex = this.selectedTabIndex;
    const selectedCss = 'tw-border-blue-500 tw-text-blue-600';
    const defaultCss = 'tw-border-transparent tw-text-gray-500 hover:tw-text-gray-700 hover:tw-border-gray-300';
    const activitiesTabCssClass = selectedTabIndex == 0 ? selectedCss : defaultCss;

    return (

      <div class="activity-list tw-absolute tw-inset-0 tw-overflow-hidden">
        <div class="tw-h-full tw-flex tw-flex-col">
          <div class="tw-border-b tw-border-gray-200">
            <nav class="-tw-mb-px tw-flex" aria-label="Tabs">
              <a href="#"
                 onClick={e => this.onTabSelected(e, 0)}
                 class={`${activitiesTabCssClass} tw-w-1/2 tw-py-4 tw-px-1 tw-text-center tw-border-b-2 tw-font-medium tw-text-sm`}>
                Activities
              </a>
            </nav>
          </div>

          <div class="tw-flex-1 tw-relative">
            <div class="tw-absolute tw-inset-0 tw-overflow-y-scroll">
              <elsa-workflow-definition-editor-toolbox-activities
                isReadonly={this.isReadonly}
                graph={this.graph}
                class={selectedTabIndex == 0 ? '' : 'hidden'}/>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
