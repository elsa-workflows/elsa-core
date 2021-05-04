import {h} from "@stencil/core";

export const SignalReceivedIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-rose-50 text-rose-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
        <path stroke="none" d="M0 0h24v24H0z"/>  <polyline points="21 12 17 12 14 20 10 4 7 12 3 12"/>
      </svg>
    </span>`
  );
