import {Component, h} from "@stencil/core";
import {Container} from "typedi";
import {EventBus} from "../../services";
import {ToolbarDisplayingArgs, ToolbarEventTypes} from "../../components/toolbar/workflow-toolbar-menu/models";

@Component({
  tag: 'elsa-labels-widget',
  shadow: false,
})
export class LabelsWidget {
  private labelsManager: HTMLElsaLabelsManagerElement;

  constructor() {
    const eventBus = Container.get(EventBus);
    eventBus.on(ToolbarEventTypes.Displaying, this.onToolbarDisplaying)
  }

  render() {
    return <elsa-labels-manager ref={el => this.labelsManager = el}/>
  }

  private onToolbarDisplaying = (e: ToolbarDisplayingArgs) => {
    e.menu.menuItems.push({
      text: 'Labels',
      onClick: this.onLabelsMenuItemClicked,
      order: 3
    });
  }

  private onLabelsMenuItemClicked = async () => {
    await this.labelsManager.show();
  };
}
