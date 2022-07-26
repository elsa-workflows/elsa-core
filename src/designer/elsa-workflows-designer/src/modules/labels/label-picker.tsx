import {Component, h, Listen, Prop, State, Element, Event, EventEmitter, Watch} from "@stencil/core";
import {TinyColor} from "@ctrl/tinycolor";
import {debounce} from 'lodash';
import {leave, toggle, enter} from 'el-transition';
import {Label} from "./models";
import labelStore from './label-store';
import {Badge} from "../../components/shared/badge/badge";
import {ConfigIcon} from "../../components/icons/tooling/config";
import {TickIcon} from "../../components/icons/tooling/tick";
import {isNullOrWhitespace} from "../../utils";

@Component({
  tag: 'elsa-label-picker',
  shadow: false,
})
export class LabelPicker {
  @Element() private element: HTMLElement;
  private searchTextElement: HTMLInputElement;
  private flyoutPanel: HTMLElement;
  private readonly filterLabelsDebounced: () => void;

  constructor() {
    this.filterLabelsDebounced = debounce(this.filterLabels, 200);
    this.filteredLabels = labelStore.labels;
  }

  @Prop() public selectedLabels: Array<string> = [];
  @Prop() public buttonClass?: string = 'text-blue-500 hover:text-blue-300';
  @Prop() public containerClass?: string;

  @Event() public selectedLabelsChanged: EventEmitter<Array<string>>;

  @State() private selectedLabelsState: Array<string> = [];
  @State() private searchText?: string;
  @State() private filteredLabels: Array<Label>;

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeFlyoutPanel();
  }

  public render() {
    const selectedLabels = this.getFilteredSelectedLabels();

    return <div class={`flex ${this.containerClass}`}>
      <div class="flex flex-grow">
        {selectedLabels.map(this.renderLabel)}
      </div>
      <div class="relative">
        <button onClick={e => this.toggleFlyoutPanel()} class={this.buttonClass}>
          <ConfigIcon/>
        </button>
        {this.renderFlyout()}
      </div>
    </div>
  }

  private renderFlyout = () => {
    const selectedLabels = this.selectedLabels;
    const labels = this.filteredLabels;
    const searchText = this.searchText;

    return <div ref={el => this.flyoutPanel = el} class="absolute z-10 right-0 transform mt-3 px-2 w-screen max-w-md px-0 hidden"
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
            <input class="h-12 w-full border-0 bg-transparent pl-11 pr-4 text-gray-800 placeholder-gray-400 focus:ring-0 sm:text-sm"
                   placeholder="Search..."
                   role="combobox"
                   type="text"
                   ref={el => this.searchTextElement = el}
                   onInput={e => this.onSearchTextChanged(e)}
                   value={searchText}/></div>

          <ul class="max-h-96 scroll-py-3 overflow-y-auto p-1" role="listbox">
            {labels.map(label => {

              const color = new TinyColor(label.color).lighten(40).toHexString();
              const colorStyle = {backgroundColor: color};
              const isSelected = !!selectedLabels.find(x => x == label.id);

              return (
                <li role="option" tab-index="-1">
                  <a class="block select-none rounded-xl p-3 bg-white hover:bg-gray-100" href="#" onClick={e => this.onLabelClick(e, label)}>
                    <div class="flex justify-start gap-1.5">
                      <div class="flex-none w-8">
                        {isSelected ? <TickIcon/> : undefined}
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

  private renderLabel = (labelId: string) => {
    const label = labelStore.labels.find(x => x.id == labelId);
    return <div class="mr-2">
      <Badge text={label.name} color={label.color}/>
    </div>
  };

  private closeFlyoutPanel = () => {
    if (!!this.flyoutPanel)
      leave(this.flyoutPanel);
  };

  private toggleFlyoutPanel = () => {
    this.filterLabelsDebounced();
    this.searchText = null;
    toggle(this.flyoutPanel);

    if (!!this.searchTextElement)
      this.searchTextElement.value = '';
      this.searchTextElement.focus();
  };

  private filterLabels = () => {
    const searchText = this.searchText;

    if (isNullOrWhitespace(searchText)) {
      this.filteredLabels = labelStore.labels;
      return;
    }

    const s = searchText.toLocaleLowerCase();
    this.filteredLabels = labelStore.labels.filter(x => x.name.toLocaleLowerCase().includes(s) || x.description.toLocaleLowerCase().includes(s));
  };

  private getFilteredSelectedLabels = (): Array<string> => {
    const labels = labelStore.labels;
    return this.selectedLabels.filter(labelId => !!labels.find(x => x.id == labelId));
  };

  private onLabelClick = (e: Event, label: Label) => {
    if (!this.selectedLabels.find(x => x == label.id))
      this.selectedLabels = [...this.selectedLabels, label.id];
    else
      this.selectedLabels = this.selectedLabels.filter(x => x != label.id);

    const selectedLabels = this.getFilteredSelectedLabels();
    this.selectedLabels = selectedLabels;
    this.selectedLabelsChanged.emit(selectedLabels);
  };

  private onSearchTextChanged = (e: Event) => {
    const value = (e.target as HTMLInputElement).value.trim();
    this.searchText = value;

    this.filterLabelsDebounced();
  };
}


