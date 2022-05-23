import {Component, FunctionalComponent, h, Prop, State} from "@stencil/core";
import {leave, toggle, enter} from 'el-transition';
import {EventTypes, Label} from "../../../models";
import {Container} from "typedi";
import {ElsaApiClientProvider, EventBus} from "../../../services";
import {Badge} from "../badge/badge";
import {ConfigIcon} from "../../icons/tooling/config";
import {TickIcon} from "../../icons/tooling/tick";
import {TinyColor} from "@ctrl/tinycolor";

@Component({
  tag: 'elsa-label-picker',
  shadow: false,
})
export class LabelPicker {
  private eventBus: EventBus;
  private flyoutPanel: HTMLElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
  }

  @Prop() selectedLabels: Array<string> = [];
  @State() availableLabels: Array<Label> = [];

  connectedCallback() {
    this.eventBus.on(EventTypes.Labels.Updated, this.onLabelsUpdated);
  }

  disconnectedCallback() {
    this.eventBus.detach(EventTypes.Labels.Updated, this.onLabelsUpdated);
  }

  async componentWillLoad() {
    await this.loadLabels();
  }

  public render() {
    const labels = this.availableLabels;

    return <div class="flex">
      <div class="flex flex-grow">
        {labels.map(this.renderLabel)}
      </div>
      <div class="relative">
        <button>
          <ConfigIcon/>
        </button>
        {this.renderFlyout()}
      </div>
    </div>
  }

  private renderFlyout = () => {
    const labels = this.availableLabels;

    return <div ref={el => this.flyoutPanel = el} class="absolute z-10 right-0 transform mt-3 px-2 w-screen max-w-md px-0"
                data-transition-enter="transition ease-out duration-200"
                data-transition-enter-start="opacity-0 translate-y-1"
                data-transition-enter-end="opacity-100 translate-y-0"
                data-transition-leave="transition ease-in duration-150"
                data-transition-leave-start="opacity-100 translate-y-0"
                data-transition-leave-end="opacity-0 translate-y-1"
    >
      <div class="rounded-lg shadow-lg ring-1 ring-black ring-opacity-5 overflow-hidden">
        <div class="mx-auto max-w-3xl transform divide-y divide-gray-100 overflow-hidden rounded-xl bg-white shadow-2xl ring-1 ring-black ring-opacity-5 transition-all opacity-100 scale-100">
          <div class="relative">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true" class="pointer-events-none absolute top-3.5 left-4 h-5 w-5 text-gray-400">
              <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/>
            </svg>
            <input class="h-12 w-full border-0 bg-transparent pl-11 pr-4 text-gray-800 placeholder-gray-400 focus:ring-0 sm:text-sm" placeholder="Search..." role="combobox" type="text" aria-expanded="true" value=""/></div>

          <ul class="max-h-96 scroll-py-3 overflow-y-auto p-1" role="listbox">
            {labels.map(label => {

              const color = new TinyColor(label.color).lighten(40).toHexString();
              const colorStyle = {backgroundColor: color};

              return (
                <li role="option" tab-index="-1">
                  <a class="block select-none rounded-xl p-3 bg-white hover:bg-gray-100" href="#">
                    <div class="flex justify-start gap-1.5">
                      <div class="flex-none w-8">
                        <TickIcon/>
                      </div>
                      <div class="flex-grow ">
                        <div class="flex gap-1.5">
                          <div class="flex-shrink-0 flex flex-col justify-center ">
                            <div class="w-4 h-4 rounded-full" style={colorStyle} aria-hidden="true"/>
                          </div>
                          <div class="flex-grow">
                            <p class="text-sm font-medium text-gray-900 font-bold">{label.name}</p>
                          </div>
                        </div>
                        <div>
                          <p class="text-sm font-normal text-gray-500">{label.description}</p>
                        </div>
                      </div>
                    </div>
                  </a>
                </li>
              );
            })}
          </ul>
        </div>
      </div>
    </div>;
  };

  private loadLabels = async () => {
    const elsaClient = await Container.get(ElsaApiClientProvider).getClient();
    this.availableLabels = await elsaClient.labels.list();
  };

  private renderLabel = (label: Label) => {
    return <div class="mr-2">
      <Badge text={label.name} color={label.color}/>
    </div>
  }

  private showFlyoutPanel() {
    if (!!this.flyoutPanel)
      enter(this.flyoutPanel);
  }

  private closeFlyoutPanel() {
    if (!!this.flyoutPanel)
      leave(this.flyoutPanel);
  }

  private toggleFlyoutPanel() {
    toggle(this.flyoutPanel);
  }

  private onLabelsUpdated = async () => {
    await this.loadLabels();
  }
}


