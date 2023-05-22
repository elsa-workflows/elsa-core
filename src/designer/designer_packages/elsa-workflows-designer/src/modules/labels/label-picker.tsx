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
  @Prop() public buttonClass?: string = 'tw-text-blue-500 hover:tw-text-blue-300';
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

    return <div class={`tw-flex ${this.containerClass}`}>
      <div class="tw-flex tw-flex-grow">
        {selectedLabels.map(this.renderLabel)}
      </div>
      <div class="tw-relative">
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

    return <div ref={el => this.flyoutPanel = el} class="tw-absolute tw-z-10 tw-right-0 tw-transform tw-mt-3 tw-px-2 tw-w-screen tw-max-w-md tw-px-0 hidden"
                data-transition-enter="tw-transition tw-ease-out tw-duration-200"
                data-transition-enter-start="tw-opacity-0 tw-translate-y-1"
                data-transition-enter-end="tw-opacity-100 tw-translate-y-0"
                data-transition-leave="tw-transition tw-ease-in tw-duration-150"
                data-transition-leave-start="tw-opacity-100 tw-translate-y-0"
                data-transition-leave-end="tw-opacity-0 tw-translate-y-1"
    >
      <div class="tw-rounded-lg tw-shadow-lg tw-ring-1 tw-ring-black tw-ring-opacity-5 tw-overflow-hidden">
        <div class="tw-mx-auto tw-max-w-3xl tw-transform tw-divide-y tw-divide-gray-100 tw-overflow-hidden tw-rounded-xl tw-bg-white tw-shadow-2xl tw-ring-1 tw-ring-black tw-ring-opacity-5 tw-transition-all tw-opacity-100 tw-scale-100">
          <div class="tw-relative">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true" class="tw-pointer-events-none tw-absolute top-3.5 left-4 tw-h-5 tw-w-5 tw-text-gray-400">
              <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/>
            </svg>
            <input class="tw-h-12 tw-w-full tw-border-0 tw-bg-transparent tw-pl-11 tw-pr-4 tw-text-gray-800 tw-placeholder-gray-400 focus:tw-ring-0 sm:tw-text-sm"
                   placeholder="Search..."
                   role="combobox"
                   type="text"
                   ref={el => this.searchTextElement = el}
                   onInput={e => this.onSearchTextChanged(e)}
                   value={searchText}/></div>

          <ul class="tw-max-h-96 tw-scroll-py-3 tw-overflow-y-auto tw-p-1" role="listbox">
            {labels.map(label => {

              const color = new TinyColor(label.color).lighten(40).toHexString();
              const colorStyle = {backgroundColor: color};
              const isSelected = !!selectedLabels.find(x => x == label.id);

              return (
                <li role="option" tab-index="-1">
                  <a class="tw-block tw-select-none tw-rounded-xl tw-p-3 tw-bg-white hover:tw-bg-gray-100" href="#" onClick={e => this.onLabelClick(e, label)}>
                    <div class="tw-flex tw-justify-start tw-gap-1.5">
                      <div class="tw-flex-none tw-w-8">
                        {isSelected ? <TickIcon/> : undefined}
                      </div>
                      <div class="tw-flex-grow ">
                        <div class="tw-flex tw-gap-1.5">
                          <div class="tw-flex-shrink-0 tw-flex tw-flex-col tw-justify-center ">
                            <div class="tw-w-4 tw-h-4 tw-rounded-full" style={colorStyle} aria-hidden="true"/>
                          </div>
                          <div class="tw-flex-grow">
                            <p class="tw-text-sm tw-font-medium tw-text-gray-900 tw-font-bold">{label.name}</p>
                          </div>
                        </div>
                        <div>
                          <p class="tw-text-sm tw-font-normal tw-text-gray-500">{label.description}</p>
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
    return <div class="tw-mr-2">
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


