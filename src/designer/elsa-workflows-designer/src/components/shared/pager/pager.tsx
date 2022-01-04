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

    const from = page * pageSize + 1;
    const to = Math.min(from + pageSize - 1, totalCount);
    const pageCount = Math.round(((totalCount - 1) / pageSize) + 0.5);

    return (
      <div class="bg-white px-4 py-3 flex items-center justify-between border-t border-gray-200 sm:px-6">
        <div class="flex-1 flex justify-between sm:hidden">
          <PreviousButton currentPage={page} pageCount={pageCount} onNavigate={this.onNavigate}/>
          <NextButton currentPage={page} pageCount={pageCount} onNavigate={this.onNavigate}/>
        </div>
        <div class="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
          <div>
            <p class="text-sm leading-5 text-gray-700 space-x-0.5">
              <span>From</span>
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
