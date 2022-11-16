import {Component, Host, h, Prop, State, Method} from "@stencil/core";
import {enter, leave} from "el-transition"
import {eventBus} from "../../../services";
import {EventTypes} from "../../../models";

@Component({
  tag: 'elsa-flyout-panel',
  shadow: false,
})

export class ElsaFlyoutPanel {
  @Prop() expandButtonPosition = 1;
  @Prop() autoExpand = false;
  @Prop() hidden = false;
  @Prop() silent = false;
  @Prop() updateCounter = 0; // This is required, so that the component would update tab click events when tabs change from outside
  @State() expanded: boolean;
  @State() currentTab: string;
  headerTabs: HTMLElsaTabHeaderElement[];
  contentTabs: HTMLElsaTabContentElement[];
  el: HTMLElement;

  async componentDidLoad() {
    this.expanded = this.autoExpand;
    this.updateTabs();
  }

  async updateTabs() {
    this.headerTabs = Array.from(this.el.querySelectorAll('elsa-tab-header'));
    this.headerTabs.forEach(element => {
      element.onclick = () => {
        this.selectTab(element.tab);
        if (!this.silent) {
          eventBus.emit(EventTypes.FlyoutPanelTabSelected, this, element.tab);
        }
      };
    })
    this.contentTabs = Array.from(this.el.querySelectorAll('elsa-tab-content'));
    if (this.headerTabs.length > 0) {
      this.currentTab = this.headerTabs[0].tab;
      await this.selectTab(this.currentTab);
    }
  }

  async componentDidRender() {
    this.updateTabs();
  }

  render() {
    const {hidden, expanded, expandButtonPosition} = this;
    const expandPositionClass = `elsa-right-${16 * (expandButtonPosition - 1) + 12}`;
    const hideOpenToggle = hidden || expanded;
    const hideContents = hidden || !expanded;

    return (
      <Host>
        <button type="button"
                onClick={this.toggle}
                class={`${hideOpenToggle ? "elsa-hidden" : expandPositionClass} workflow-settings-button elsa-fixed elsa-top-20 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 elsa-z-10`}>
          <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-8 elsa-w-8" fill="none" viewBox="0 0 24 24"
               stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 19l-7-7 7-7m8 14l-7-7 7-7"/>
          </svg>
        </button>
        <section
          class={`${hideContents ? 'elsa-hidden' : ''} elsa-fixed elsa-top-4 elsa-right-0 elsa-bottom-0 elsa-overflow-hidden`}
          aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
          <div class="elsa-absolute elsa-inset-0 elsa-overflow-hidden">
            <div class="elsa-absolute elsa-inset-0" aria-hidden="true"/>
            <div
              class="elsa-fixed elsa-top-20 elsa-inset-y-0 elsa-right-2 elsa-bottom-2 max-elsa-w-full elsa-flex">
              <div
                ref={el => this.el = el}
                data-transition-enter="elsa-transform elsa-transition elsa-ease-in-out elsa-duration-300 sm:elsa-duration-700"
                data-transition-enter-start="elsa-translate-x-full"
                data-transition-enter-end="elsa-translate-x-0"
                data-transition-leave="elsa-transform elsa-transition elsa-ease-in-out elsa-duration-300 sm:elsa-duration-700"
                data-transition-leave-start="elsa-translate-x-0"
                data-transition-leave-end="elsa-translate-x-full"
                class="elsa-w-screen elsa-max-w-lg elsa-h-full ">
                <button type="button"
                        onClick={this.toggle}
                        class={`${this.autoExpand ? 'elsa-hidden' : ''} workflow-settings-button elsa-absolute elsa-left-2 elsa-inline-flex elsa-items-center elsa-p-2 elsa-rounded-full elsa-border elsa-border-transparent elsa-bg-white shadow elsa-text-gray-400 hover:elsa-text-blue-500 focus:elsa-text-blue-500 hover:elsa-ring-2 hover:elsa-ring-offset-2 hover:elsa-ring-blue-500 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 elsa-z-10`}>
                  <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-8 elsa-w-8" fill="none" viewBox="0 0 24 24"
                       stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M13 5l7 7-7 7M5 5l7 7-7 7"/>
                  </svg>
                </button>
                <div
                  class="elsa-h-full elsa-flex elsa-flex-col elsa-py-6 elsa-bg-white elsa-shadow-xl elsa-bg-white">
                  <div class="elsa-h-full elsa-mt-8 elsa-p-6 elsa-flex elsa-flex-col">
                      <div class="elsa-border-b elsa-border-gray-200">
                        <nav class="-elsa-mb-px elsa-flex elsa-space-x-8" aria-label="Tabs">
                          <slot name="header"/>
                        </nav>
                      </div>
                      <section class="elsa-overflow-auto elsa-h-full">
                        <slot name="content"/>
                      </section>
                    </div>
                  </div>
                </div>
              </div>
          </div>
        </section>
      </Host>
    );
  }

  toggle = () => {
    if (this.expanded) {
      leave(this.el).then(() => this.expanded = false);
    } else {
      this.expanded = true;
      enter(this.el);
    }
  }

  @Method()
  async selectTab(tab: string, expand = false) {
    this.headerTabs.forEach(element => {
      element.active = element.tab === tab;
    });
    this.contentTabs.forEach(element => {
      element.active = element.tab === tab;
    });

    if (expand && !this.expanded) {
      this.expanded = true;
      enter(this.el);
    }
  }
}
