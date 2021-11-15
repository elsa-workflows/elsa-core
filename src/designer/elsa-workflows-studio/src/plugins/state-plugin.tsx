import {eventBus, ElsaPlugin} from "../services";
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";
import {htmlEncode, parseJson} from "../utils/utils";

export class StatePlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'State')
      return;

    const props = activityModel.properties || [];
    const stateNameProp = props.find(x => x.name == 'StateName') || { name: 'Text', expressions: {'Literal': ''}, syntax: SyntaxNames.Literal };
    context.displayName = htmlEncode(stateNameProp.expressions[stateNameProp.syntax || 'Literal'] || 'State');
  }
}
