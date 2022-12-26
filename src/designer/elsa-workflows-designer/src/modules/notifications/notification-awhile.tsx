import {Component, h, State, Prop, Watch} from "@stencil/core";
import {NotificationType} from "./models";

@Component({
    tag: 'elsa-awhile-notifications',
    shadow: false,
    styleUrl: '',
})
export class NotificationAwhile {
    @State() private isOpened = true;
    @Prop() public notification: NotificationType;

    @Watch('notification')
    onNotificationChange(){
      console.log('sss')
    }

    componentDidRender() {
        setTimeout(() => {
            this.isOpened = false;
        }, 6000)
    }

    render() {
      console.log(this.notification);

      const {title, text} = this.notification;

        return (
          this.isOpened ? (
            <div class='flex w-full flex-col items-center space-y-4 sm:items-end  z-30'>
              <div
                class="pointer-events-auto w-full max-w-sm rounded-lg z-50 bg-white shadow-lg ring-1 ring-black ring-opacity-5">
                <div class="p-4 z-30">
                  <div class="flex items-start z-30">
                    <div class="flex-shrink-0 z-30">
                      <svg class="h-6 w-6 text-green-400 z-30" xmlns="http://www.w3.org/2000/svg" fill="none"
                           viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round"
                              d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                      </svg>
                    </div>
                    <div class="ml-3 w-0 flex-1 pt-0.5  z-30">
                      <p class="text-sm font-medium text-gray-900">{title}</p>
                      <p class="mt-1 text-sm text-gray-500">{text}</p>
                    </div>
                    <div class="ml-4 flex flex-shrink-0 z-30">
                      <button
                        type="button"
                        // onclick={() => console.log(id)}
                        class="inline-flex rounded-md bg-white text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
                        <span class="sr-only">Close</span>
                        <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"
                             fill="currentColor" aria-hidden="true">
                          <path
                            d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
                        </svg>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
        </div> )
            : null
        )
    }
}
