import {h} from "@stencil/core";

export const SignalReceivedIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-rose-50 text-rose-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
      </svg>
    </span>`
  );
