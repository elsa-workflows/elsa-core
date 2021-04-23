import {h} from "@stencil/core";

export const SendSignalIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="2"/>
        <path d="M16.24 7.76a6 6 0 0 1 0 8.49m-8.48-.01a6 6 0 0 1 0-8.49m11.31-2.82a10 10 0 0 1 0 14.14m-14.14 0a10 10 0 0 1 0-14.14"/>
      </svg>
    </span>`
  );
