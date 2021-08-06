import {eventBus, ElsaPlugin} from "../services";
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";
import {htmlEncode} from "../utils/utils";

export class HttpEndpointPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
  }

  onActivityDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;
    
    if (activityModel.type !== 'HttpEndpoint')
      return;
      
    const props = activityModel.properties || [];
    const path = props.find(x => x.name == 'Path') || { name: 'Path', expressions: { 'Literal': '', syntax: SyntaxNames.Literal } };
    const syntax = path.syntax || SyntaxNames.Literal;
    const bodyDisplay = htmlEncode(path.expressions[syntax]);
    context.bodyDisplay = `<p>${bodyDisplay}</p>`;
  }
}
