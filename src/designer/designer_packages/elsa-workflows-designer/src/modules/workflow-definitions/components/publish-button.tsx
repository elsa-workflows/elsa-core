import {Component, Event, EventEmitter, h, Prop} from '@stencil/core';
import {DropdownButtonItem} from "../../../components/shared/dropdown-button/models";
import NotificationService from "../../notifications/notification-service";

export interface PublishClickedArgs {
  begin: () => void;
  complete: () => void;
}

@Component({
  tag: 'elsa-workflow-publish-button',
  shadow: false,
})
export class PublishButton {

  @Prop() publishing: boolean;
  @Event({bubbles: true}) newClicked: EventEmitter;
  @Event({bubbles: true}) publishClicked: EventEmitter<PublishClickedArgs>;
  @Event({bubbles: true}) unPublishClicked: EventEmitter;
  @Event({bubbles: true}) exportClicked: EventEmitter;
  @Event({bubbles: true}) importClicked: EventEmitter<File>;

  menu: HTMLElement;
  element: HTMLElement;

  private onPublishClick() {
    this.publishClicked.emit({
      begin: () => this.publishing = true,
      complete: () => this.publishing = false
    });
  }

  private onUnPublishClick() {
    this.unPublishClicked.emit();
  }

  private async onExportClick() {
    this.exportClicked.emit();
  }

  private async onImportClick() {
    this.importClicked.emit();
  }

  private publishingIcon() {
    if(!this.publishing)
      return null;

    return <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-blue-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
    </svg>;
  }

  render() {
    const publishing = this.publishing;

    const items: Array<DropdownButtonItem> = [{
      text: 'Export',
      handler: () => this.onExportClick(),
      group: 1
    },{
      text: 'Import',
      handler: () => this.onImportClick(),
      group: 1
    }, {
      text: 'Unpublish',
      handler: () => this.onUnPublishClick(),
      group: 2
    }];

    const mainItem: DropdownButtonItem = {
      text: publishing ? 'Publishing' : 'Publish',
      handler: publishing ? () => {} : () => this.onPublishClick()
    }

    return <elsa-dropdown-button text={mainItem.text} handler={mainItem.handler} items={items} icon={this.publishingIcon()} onMenuOpened={() => NotificationService.hideAllNotifications()} />
  }
}
