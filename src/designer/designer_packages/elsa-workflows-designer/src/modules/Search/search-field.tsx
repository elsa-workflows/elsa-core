import { Component, h, Listen, Prop, State, Element, Event, EventEmitter, Watch } from "@stencil/core";
import { TinyColor } from "@ctrl/tinycolor";

@Component({
  tag: 'search-field',
  shadow: false,
})
export class SearchField {

  private searchTextElement: HTMLInputElement;

  constructor() {

  }

  @Prop() public selectedLabels: Array<string> = [];
  @Prop() public buttonClass?: string = 'tw-text-blue-500 hover:tw-text-blue-300';
  @Prop() public containerClass?: string;
  @State() private searchText?: string;

  public render() {
    const searchText = this.searchText;
    return <div class="tw-z-0 tw-shadow-sm tw-rounded-md tw-w-3/6">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"
        class="tw-pointer-events-none tw-absolute tw-h-5 tw-w-5 tw-text-gray-400">
        <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd" />
      </svg>
        <input
        class="tw-px-10"
        placeholder="Search..."
        role="combobox"
        type="text"
        ref={el => this.searchTextElement = el}
        onInput={e => this.onSearchTextChanged(e)}
          value={searchText} />
      </div>
  
  }


  private onSearchTextChanged = (e: Event) => {
    const value = (e.target as HTMLInputElement).value.trim();
    this.searchText = value;

    alert(this.searchText);
    // do something
  };
}


