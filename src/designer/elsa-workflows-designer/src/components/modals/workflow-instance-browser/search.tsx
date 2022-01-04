import {FunctionalComponent, h} from "@stencil/core";
import {debounce} from 'lodash';

export interface SearchProps {
  onSearch: (term: string) => void;
}

export const Search: FunctionalComponent<SearchProps> = ({onSearch}) => {

  const onSubmit = (e: Event) => {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const searchTerm: FormDataEntryValue = formData.get('searchTerm').toString();

    onSearch(searchTerm);
  }

  const onSearchDebounced = debounce(onSearch, 200);

  const onKeyUp = (e: KeyboardEvent) => {
    const term = (e.target as HTMLInputElement).value;
    onSearchDebounced(term);
  };

  return <div class="relative z-10 flex-shrink-0 flex h-16 bg-white border-b border-gray-200">
    <div class="flex-1 px-4 flex justify-between sm:px-6 lg:px-8">
      <div class="flex-1 flex">
        <form class="w-full flex md:ml-0" onSubmit={onSubmit}>
          <label htmlFor="search_field" class="sr-only">Search</label>
          <div class="relative w-full text-gray-400 focus-within:text-gray-600">
            <div
              class="absolute inset-y-0 left-0 flex items-center pointer-events-none">
              <svg class="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" clip-rule="evenodd"
                      d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"/>
              </svg>
            </div>
            <input name="searchTerm"
                   onKeyUp={onKeyUp}
                   class="block w-full h-full pl-8 pr-3 py-2 rounded-md text-gray-900 placeholder-cool-gray-500 focus:placeholder-cool-gray-400 sm:text-sm border-0 focus:outline-none focus:ring-0"
                   placeholder="Search"
                   type="search"/>
          </div>
        </form>
      </div>
    </div>
  </div>;
}
