import {Component, Event, EventEmitter, h, Prop} from '@stencil/core';
import {LocationSegments, RouterHistory, injectHistory} from "@stencil/router";
import {parseQuery, queryToString} from "../../../utils/utils";
import {i18n} from "i18next";
import {resources} from "./localizations";
import {loadTranslations} from "../../i18n/i18n-loader";
import {GetIntlMessage} from "../../i18n/intl-message";

export interface PagerData {
  page: number;
  pageSize: number;
  totalCount: number;
}

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
  @Prop() culture: string;

  @Event() paged: EventEmitter<PagerData>

  i18next: i18n;
  basePath: string;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
    this.basePath = !!this.location ? this.location.pathname : document.location.pathname;
  }

  t = (key: string, options?: any) => this.i18next.t(key, options);

  navigate(path: string, page: number) {

    if (this.history) {
      this.history.push(path);
      return;
    } else {
      this.paged.emit({page, pageSize: this.pageSize, totalCount: this.totalCount});
    }
  }

  onNavigateClick(e: Event, page: number) {
    const anchor = e.currentTarget as HTMLAnchorElement;

    e.preventDefault();
    this.navigate(`${anchor.pathname}${anchor.search}`, page);
  }

  render() {
    const page = this.page;
    const pageSize = this.pageSize;
    const totalCount = this.totalCount;
    const basePath = this.basePath;

    const from = page * pageSize + 1;
    const to = Math.min(from + pageSize - 1, totalCount);
    const pageCount = Math.round(((totalCount - 1) / pageSize) + 0.5);

    const maxPageButtons = 10;
    const fromPage = Math.max(0, page - maxPageButtons / 2);
    const toPage = Math.min(pageCount, fromPage + maxPageButtons);
    const self = this;
    const currentQuery = !!this.history ? parseQuery(this.history.location.search) : {page, pageSize};
    const t = this.t;
    const IntlMessage = GetIntlMessage(this.i18next);

    currentQuery['pageSize'] = pageSize;

    const getNavUrl = (page: number) => {
      const query = {...currentQuery, 'page': page};
      return `${basePath}?${queryToString(query)}`;
    };

    const renderPreviousButton = function () {
      if (page <= 0)
        return;

      return <a href={`${getNavUrl(page - 1)}`}
                onClick={e => self.onNavigateClick(e, page - 1)}
                class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-gray-300 elsa-text-sm elsa-leading-5 elsa-font-medium elsa-rounded-md elsa-text-gray-700 elsa-bg-white hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-shadow-outline-blue focus:elsa-border-blue-300 active:elsa-bg-gray-100 active:elsa-text-gray-700 elsa-transition elsa-ease-in-out elsa-duration-150">
        {t('Previous')}
      </a>
    }

    const renderNextButton = function () {
      if (page >= pageCount)
        return;

      return <a href={`/${getNavUrl(page + 1)}`}
                onClick={e => self.onNavigateClick(e, page + 1)}
                class="elsa-ml-3 elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-gray-300 elsa-text-sm elsa-leading-5 elsa-font-medium elsa-rounded-md elsa-text-gray-700 elsa-bg-white hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-shadow-outline-blue focus:elsa-border-blue-300 active:elsa-bg-gray-100 active:elsa-text-gray-700 elsa-transition elsa-ease-in-out elsa-duration-150">
        {t('Next')}
      </a>
    }

    const renderChevronLeft = function () {
      if (page <= 0)
        return;

      return (
        <a href={`${getNavUrl(page - 1)}`}
           onClick={e => self.onNavigateClick(e, page - 1)}
           class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-2 elsa-py-2 elsa-rounded-l-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-500 hover:elsa-text-gray-400 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-border-blue-300 focus:elsa-shadow-outline-blue active:elsa-bg-gray-100 active:elsa-text-gray-500 elsa-transition elsa-ease-in-out elsa-duration-150"
           aria-label="Previous">
          <svg class="elsa-h-5 elsa-w-5" x-description="Heroicon name: chevron-left" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd"/>
          </svg>
        </a>);
    }

    const renderChevronRight = function () {
      if (page >= pageCount - 1)
        return;

      return (
        <a href={`${getNavUrl(page + 1)}`}
           onClick={e => self.onNavigateClick(e, page + 1)}
           class="-elsa-ml-px elsa-relative elsa-inline-flex elsa-items-center elsa-px-2 elsa-py-2 elsa-rounded-r-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-500 hover:elsa-text-gray-400 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-border-blue-300 focus:elsa-shadow-outline-blue active:elsa-bg-gray-100 active:elsa-text-gray-500 elsa-transition elsa-ease-in-out elsa-duration-150"
           aria-label="Next">
          <svg class="elsa-h-5 elsa-w-5" x-description="Heroicon name: chevron-right" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
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
        const leftRoundedClass = isFirst && isCurrent ? 'elsa-rounded-l-md' : '';
        const rightRoundedClass = isLast && isCurrent ? 'elsa-rounded-r-md' : '';

        if (isCurrent) {
          buttons.push(<span
            class={`-elsa-ml-px elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-gray-300 elsa-text-sm elsa-leading-5 elsa-font-medium elsa-bg-blue-600 elsa-text-white ${leftRoundedClass} ${rightRoundedClass}`}>
                        {i + 1}
                    </span>);
        } else {
          buttons.push(<a href={`${getNavUrl(i)}`}
                          onClick={e => self.onNavigateClick(e, i)}
                          class={`-elsa-ml-px elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-700 hover:elsa-text-gray-500 focus:elsa-z-10 focus:elsa-outline-none active:elsa-bg-gray-100 active:elsa-text-gray-700 elsa-transition elsa-ease-in-out elsa-duration-150 ${leftRoundedClass}`}>
            {i + 1}
          </a>)
        }
      }

      return buttons;
    }

    return (
      <div class="elsa-bg-white elsa-px-4 elsa-py-3 elsa-flex elsa-items-center elsa-justify-between elsa-border-t elsa-border-gray-200 sm:elsa-px-6">
        <div class="elsa-flex-1 elsa-flex elsa-justify-between sm:elsa-hidden">
          {renderPreviousButton()}
          {renderNextButton()}
        </div>
        <div class="hidden sm:elsa-flex-1 sm:elsa-flex sm:elsa-items-center sm:elsa-justify-between">
          <div>
            <p class="elsa-text-sm elsa-leading-5 elsa-text-gray-700 elsa-space-x-0-5">
              <span>{t('From')}</span>
              <span class="elsa-font-medium">{from}</span>
              <span>{t('To')}</span>
              <span class="elsa-font-medium">{to}</span>
              <span>{t('Of')}</span>
              <span class="elsa-font-medium">{totalCount}</span>
              <span>{t('Results')}</span>
            </p>
          </div>
          <div>
            <nav class="elsa-relative elsa-z-0 elsa-inline-flex elsa-shadow-sm">
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
