import {Component, Event, EventEmitter, h, Prop} from '@stencil/core';
import {ChevronLeft, ChevronRight, NextButton, PagerButtons, PreviousButton} from "./controls";

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
  @Event() paginated: EventEmitter<PagerData>

  public render() {
    const page = this.page;
    const pageSize = this.pageSize;
    const totalCount = this.totalCount;
    const from = totalCount == 0 ? 0 : page * pageSize + 1;
    const to = Math.min(from + pageSize - 1, totalCount);
    const pageCount = Math.round(((totalCount - 1) / pageSize) + 0.5);

    return (
      <div class="tw-bg-white tw-px-4 tw-py-3 tw-flex tw-items-center tw-justify-between tw-border-t tw-border-gray-200 sm:tw-px-6">
        <div class="tw-flex-1 tw-flex tw-justify-between sm:hidden">
          <PreviousButton currentPage={page} pageCount={pageCount} onNavigate={this.onNavigate}/>
          <NextButton currentPage={page} pageCount={pageCount} onNavigate={this.onNavigate}/>
        </div>
        <div class="hidden sm:tw-flex-1 sm:tw-flex sm:tw-items-center sm:tw-justify-between">
          <div>
            <p class="tw-text-sm tw-leading-5 tw-text-gray-700 tw-space-x-0.5">
              <span>From</span>
              <span class="tw-font-medium">{from}</span>
              <span>to</span>
              <span class="tw-font-medium">{to}</span>
              <span>of</span>
              <span class="tw-font-medium">{totalCount}</span>
              <span>results</span>
            </p>
          </div>
          <div>
            <nav class="tw-relative tw-z-0 tw-inline-flex tw-shadow-sm">
              <ChevronLeft currentPage={page} pageCount={pageCount} onNavigate={this.onNavigate}/>
              <PagerButtons currentPage={page} pageCount={pageCount} onNavigate={this.onNavigate}/>
              <ChevronRight currentPage={page} pageCount={pageCount} onNavigate={this.onNavigate}/>
            </nav>
          </div>
        </div>
      </div>
    );
  }

  private navigate = (page: number) => this.paginated.emit({page, pageSize: this.pageSize, totalCount: this.totalCount});
  private onNavigate = (page: number) => this.navigate(page);
}
