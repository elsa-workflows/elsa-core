import {Component, State, h, getAssetPath} from '@stencil/core';
import Container from 'typedi';
import {NotificationEventTypes} from '../../../modules/notifications/event-types';
import {EventBus} from '../../../services';
import toolbarComponentStore from "../../../data/toolbar-component-store";
import notificationService from '../../../modules/notifications/notification-service';
import notificationStore from "../../../modules/notifications/notification-store";
import {PackagesApi} from "../../../services/api-client/packages-api";

@Component({
  tag: 'elsa-workflow-toolbar',
  assetsDirs: ['assets']
})
export class WorkflowToolbar {
  private readonly eventBus: EventBus;
  private readonly packagesApi: PackagesApi;

  private currentElsaVersion: string;

  static NotificationService = notificationService;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.packagesApi = Container.get(PackagesApi);
  }

  async componentWillLoad() {
    var response = await this.packagesApi.getVersion();
    return this.currentElsaVersion = response.packageVersion;
  }

  onNotificationClick = async e => {
    e.stopPropagation();
    await this.eventBus.emit(NotificationEventTypes.Toggle, this);
    WorkflowToolbar.NotificationService.toggleNotification();
  };

  render() {
    const logoPath = getAssetPath('./assets/logo.png');
    const infoPanelBoolean = notificationStore.infoPanelBoolean;

    return (
      <div>
        <nav class="tw-bg-gray-800">
          <div class="tw-mx-auto tw-px-2 sm:tw-px-6 lg:tw-px-6">

            <div class="tw-flex tw-items-center tw-h-16">
              <div class="tw-flex-shrink-0">
                <div class="tw-flex tw-items-end tw-space-x-1">
                  <div>
                    <a href="#"><img class="tw-h-6 tw-w-6" src={logoPath} alt="Workflow"/></a>
                  </div>
                  <div>
                    <span class="tw-text-gray-300 tw-text-sm">{this.currentElsaVersion}</span>
                  </div>
                </div>
              </div>
              <div class="tw-flex-grow"></div>

              <div class="tw-relative tw-flex tw-items-center tw-justify-end tw-h-16">
                <div class="tw-absolute tw-inset-y-0 tw-right-0 tw-flex tw-items-center tw-pr-2 sm:tw-static sm:tw-inset-auto sm:tw-ml-6 sm:tw-pr-0 tw-z-40">
                  <div class="tw-inset-y-0 tw-right-0 tw-flex tw-items-center tw-pr-2 sm:tw-static sm:tw-inset-auto sm:tw-ml-6 sm:tw-pr-0 tw-z-40">
                    {/* Notifications*/}
                    <button
                      onClick={e => this.onNotificationClick(e)}
                      type="button"
                      class="tw-bg-gray-800 tw-p-1 tw-rounded-full tw-text-gray-400 hover:tw-text-white focus:tw-outline-none focus:tw-ring-2 focus:tw-ring-offset-2 focus:tw-ring-offset-gray-800 focus:tw-ring-white tw-mr-4"
                    >
                      <span class="tw-sr-only">View notifications</span>
                      <svg class="tw-h-6 tw-w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                        />
                      </svg>
                    </button>

                    {toolbarComponentStore.components.map(component => (
                      <div class="tw-flex-shrink-0 tw-mr-4">
                        {component()}
                      </div>
                    ))}

                    {/* Menu */}
                    <elsa-workflow-toolbar-menu/>
                  </div>
                </div>
              </div>
            </div>
          </div>

        </nav>
        <elsa-notifications-manager modalState={infoPanelBoolean}></elsa-notifications-manager>
        <elsa-toast-manager></elsa-toast-manager>
      </div>
    );
  }
}
