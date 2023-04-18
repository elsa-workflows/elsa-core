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

  async componentDidLoad() {
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
        <nav class="bg-gray-800">
          <div class="mx-auto px-2 sm:px-6 lg:px-6">

            <div class="flex items-center h-16">
              <div class="flex-shrink-0">
                <div class="flex items-end space-x-1">
                  <div>
                    <a href="#"><img class="h-6 w-6" src={logoPath} alt="Workflow"/></a>
                  </div>
                  <div>
                    <span class="text-gray-300 text-sm">{this.currentElsaVersion}</span>
                  </div>
                </div>
              </div>
              <div class="flex-grow"></div>

              <div class="relative flex items-center justify-end h-16">
                <div class="absolute inset-y-0 right-0 flex items-center pr-2 sm:static sm:inset-auto sm:ml-6 sm:pr-0 z-40">
                  <div class="inset-y-0 right-0 flex items-center pr-2 sm:static sm:inset-auto sm:ml-6 sm:pr-0 z-40">
                    {/* Notifications*/}
                    <button
                      onClick={e => this.onNotificationClick(e)}
                      type="button"
                      class="bg-gray-800 p-1 rounded-full text-gray-400 hover:text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-800 focus:ring-white mr-4"
                    >
                      <span class="sr-only">View notifications</span>
                      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                        />
                      </svg>
                    </button>

                    {toolbarComponentStore.components.map(component => (
                      <div class="flex-shrink-0 mr-4">
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
