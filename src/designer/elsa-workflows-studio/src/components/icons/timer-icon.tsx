import {h} from "@stencil/core";

export const TimerIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-rose-50 text-rose-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
      </svg>
    </span>`
  );
