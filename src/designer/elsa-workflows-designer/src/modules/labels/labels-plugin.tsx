import {Plugin} from "../../models";
import {Container} from "typedi";
import {EventBus} from "../../services";
import {ToolbarDisplayingArgs, ToolbarEventTypes} from "../../components/toolbar/workflow-toolbar-menu/models";

export class LabelsPlugin implements Plugin {
  constructor() {
    // const eventBus = Container.get(EventBus);
    // eventBus.on(ToolbarEventTypes.Displaying, this.onToolbarDisplaying)
  }


}
