import {Component, Event, EventEmitter, h, Prop} from '@stencil/core';
import {LocationSegments, RouterHistory, injectHistory } from "@stencil/router";
import {parseQuery, queryToString} from "../../../utils/utils";

@Component({
    tag: 'elsa-pager',
    shadow: false,
})
export class ElsaPager {

    @Prop() page: number;
    @Prop() pageSize: number;
    @Prop() totalCount: number;
    @Prop() location: LocationSegments;
    @Prop() history?: RouterHistory;

    basePath: string;

    componentWillLoad() {
        this.basePath = !!this.location ? this.location.pathname : document.location.pathname;
    }

    navigate(path: string) {
        if (this.history) {
            this.history.push(path);
            return;
        }

        document.location.pathname = path;
    }

    onNavigateClick(e: Event) {
        const anchor = e.currentTarget as HTMLAnchorElement;

        e.preventDefault();
        this.navigate(`${anchor.pathname}${anchor.search}`);
    }

    render() {
        const page = this.page;
        const pageSize = this.pageSize;
        const totalCount = this.totalCount;
        const basePath = this.basePath;

        const from = page * pageSize + 1;
        const to = Math.min(from + pageSize, totalCount);
        const pageCount = Math.round(((totalCount - 1) / pageSize) + 0.5);

        const maxPageButtons = 10;
        const fromPage = Math.max(0, page - maxPageButtons / 2);
        const toPage = Math.min(pageCount, fromPage + maxPageButtons);
        const self = this;
        const currentQuery = parseQuery(this.history.location.search);

        currentQuery['pageSize'] = pageSize;
        
        const getNavUrl = (page: number) => {
            const query = {...currentQuery, 'page': page};
            return `${basePath}?${queryToString(query)}`;
        };

        const renderPreviousButton = function () {
            if (page <= 0)
                return;
            
            return <a href={`${getNavUrl(page - 1)}`}
                      onClick={e => self.onNavigateClick(e)}
                      class="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm leading-5 font-medium rounded-md text-gray-700 bg-white hover:text-gray-500 focus:outline-none focus:shadow-outline-blue focus:border-blue-300 active:bg-gray-100 active:text-gray-700 transition ease-in-out duration-150">
                Previous
            </a>
        }

        const renderNextButton = function () {
            if (page >= pageCount)
                return;

            return <a href={`/${getNavUrl(page + 1)}`}
                      onClick={e => self.onNavigateClick(e)}
                      class="ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm leading-5 font-medium rounded-md text-gray-700 bg-white hover:text-gray-500 focus:outline-none focus:shadow-outline-blue focus:border-blue-300 active:bg-gray-100 active:text-gray-700 transition ease-in-out duration-150">
                Next
            </a>
        }

        const renderChevronLeft = function () {
            if (page <= 0)
                return;

            return (
                <a href={`${getNavUrl(page - 1)}`}
                   onClick={e => self.onNavigateClick(e)}
                   class="relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm leading-5 font-medium text-gray-500 hover:text-gray-400 focus:z-10 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-100 active:text-gray-500 transition ease-in-out duration-150"
                   aria-label="Previous">
                    <svg class="h-5 w-5" x-description="Heroicon name: chevron-left" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd"/>
                    </svg>
                </a>);
        }

        const renderChevronRight = function () {
            if (page >= pageCount - 1)
                return;

            return (
                <a href={`${getNavUrl(page + 1)}`}
                   onClick={e => self.onNavigateClick(e)}
                   class="-ml-px relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm leading-5 font-medium text-gray-500 hover:text-gray-400 focus:z-10 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-100 active:text-gray-500 transition ease-in-out duration-150"
                   aria-label="Next">
                    <svg class="h-5 w-5" x-description="Heroicon name: chevron-right" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
                    </svg>
                </a>
            );
        }

        const renderPagerButtons = function () {
            const buttons = [];

            for (let i = fromPage; i < toPage; i++) {
                const isCurrent = page == i;
                const isFirst = i == fromPage;
                const isLast = i == toPage - 1;
                const leftRoundedClass = isFirst && isCurrent ? 'rounded-l-md' : '';
                const rightRoundedClass = isLast && isCurrent ? 'rounded-r-md' : '';

                if (isCurrent) {
                    buttons.push(<span class={`-ml-px relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm leading-5 font-medium bg-blue-600 text-white ${leftRoundedClass} ${rightRoundedClass}`}>
                        {i + 1}
                    </span>);
                } else {
                    buttons.push(<a href={`${getNavUrl(i)}`}
                                    onClick={e => self.onNavigateClick(e)}
                                    class={`-ml-px relative inline-flex items-center px-4 py-2 border border-gray-300 bg-white text-sm leading-5 font-medium text-gray-700 hover:text-gray-500 focus:z-10 focus:outline-none active:bg-gray-100 active:text-gray-700 transition ease-in-out duration-150 ${leftRoundedClass}`}>
                        {i + 1}
                    </a>)
                }
            }

            return buttons;
        }

        return (
            <div class="bg-white px-4 py-3 flex items-center justify-between border-t border-gray-200 sm:px-6">
                <div class="flex-1 flex justify-between sm:hidden">
                    {renderPreviousButton()}
                    {renderNextButton()}
                </div>
                <div class="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
                    <div>
                        <p class="text-sm leading-5 text-gray-700 space-x-0-5">
                            <span>Showing</span>
                            <span class="font-medium">{from}</span>
                            <span>to</span>
                            <span class="font-medium">{to}</span>
                            <span>of</span>
                            <span class="font-medium">{totalCount}</span>
                            <span>results</span>
                        </p>
                    </div>
                    <div>
                        <nav class="relative z-0 inline-flex shadow-sm">
                            {renderChevronLeft()}
                            {renderPagerButtons()}
                            {renderChevronRight()}
                        </nav>
                    </div>
                </div>
            </div>
        );
    }
}

injectHistory(ElsaPager);