import {FunctionalComponent, h} from "@stencil/core";

export interface PagerProps {
  currentPage: number;
  pageCount: number;
  onNavigate: (page: number) => void;
}

const clickHandler = (e: MouseEvent, onNavigate: () => void) => {
  e.preventDefault();
  onNavigate();
};

export const PreviousButton: FunctionalComponent<PagerProps> = ({currentPage, pageCount, onNavigate}) => currentPage > 0 ?
  <a href="#" onClick={e => clickHandler(e, () => onNavigate(currentPage - 1))}
     class="pager-button previous">
    Previous
  </a> : undefined;

export const NextButton: FunctionalComponent<PagerProps> = ({currentPage, pageCount, onNavigate}) => currentPage < pageCount ?
  <a href="#"
     onClick={e => clickHandler(e, () => onNavigate(currentPage + 1))}
     class="pager-button next">
    Next
  </a> : undefined;

export const ChevronLeft: FunctionalComponent<PagerProps> = ({currentPage, pageCount, onNavigate}) => currentPage > 0 ?
  <a href="#" onClick={e => clickHandler(e, () => onNavigate(currentPage - 1))} class="pager-chevron left" aria-label="Previous">
    <svg class="h-5 w-5" x-description="Heroicon name: chevron-left" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
      <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd"/>
    </svg>
  </a> : undefined;

export const ChevronRight: FunctionalComponent<PagerProps> = ({currentPage, pageCount, onNavigate}) => currentPage < pageCount - 1 ?
  <a href="#"
     onClick={e => clickHandler(e, () => onNavigate(currentPage + 1))}
     class="pager-chevron right"
     aria-label="Next">
    <svg class="h-5 w-5" x-description="Heroicon name: chevron-right" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
      <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
    </svg>
  </a> : undefined;

export const PagerButtons: FunctionalComponent<PagerProps> = ({currentPage, pageCount, onNavigate}) => {
  const buttons = [];
  const maxPageButtons = 10;
  const fromPage = Math.max(0, currentPage - maxPageButtons / 2);
  const toPage = Math.min(pageCount, fromPage + maxPageButtons);

  for (let i = fromPage; i < toPage; i++) {
    const isCurrent = currentPage == i;
    const isFirst = i == fromPage;
    const isLast = i == toPage - 1;
    const leftRoundedClass = isFirst && isCurrent ? 'rounded-l-md' : '';
    const rightRoundedClass = isLast && isCurrent ? 'rounded-r-md' : '';

    if (isCurrent) {
      buttons.push(<span
        class={`-ml-px relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm leading-5 font-medium bg-blue-600 text-white ${leftRoundedClass} ${rightRoundedClass}`}>
                        {i + 1}
                    </span>);
    } else {
      buttons.push(<a href="#"
                      onClick={e => clickHandler(e, () => onNavigate(i))}
                      class={`-ml-px relative inline-flex items-center px-4 py-2 border border-gray-300 bg-white text-sm leading-5 font-medium text-gray-700 hover:text-gray-500 focus:z-10 focus:outline-none active:bg-gray-100 active:text-gray-700 transition ease-in-out duration-150 ${leftRoundedClass}`}>
        {i + 1}
      </a>)
    }
  }

  return buttons;
}
