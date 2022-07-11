import {Component, h, Host} from "@stencil/core";
import {Container} from "typedi";
import {ElsaApiClientProvider, ElsaClient, EventBus} from "../../services";
import {ToolbarDisplayingArgs, ToolbarEventTypes} from "../../components/toolbar/workflow-toolbar-menu/models";

@Component({
  tag: 'elsa-custom-activities-manager',
  shadow: false,
})
export class CustomActivitiesManager {

  private readonly eventBus: EventBus;
  private elsaClient: ElsaClient;
  private activityDefinitionBrowser: HTMLElsaActivityDefinitionBrowserElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.eventBus.on(ToolbarEventTypes.Displaying, this.onToolbarDisplaying);
  }

  public async componentWillLoad() {
    this.elsaClient = await Container.get(ElsaApiClientProvider).getElsaClient();
  }

  private onToolbarDisplaying = (e: ToolbarDisplayingArgs) => {
    e.menu.menuItems.push({
      text: 'Custom Activities',
      onClick: this.onCustomActivitiesMenuItemClicked,
      order: 10
    });
  }

  private onCustomActivitiesMenuItemClicked = async () => await this.activityDefinitionBrowser.show();

  public render() {
    return <elsa-activity-definition-browser ref={el => this.activityDefinitionBrowser = el}/>;
  }
}
