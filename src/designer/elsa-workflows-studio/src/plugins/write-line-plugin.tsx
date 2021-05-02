import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";

export class WriteLinePlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'WriteLine')
      return;

    const props = activityModel.properties || [];
    const condition = props.find(x => x.name == 'Text') || { name: 'Text', expressions: {'Literal': ''}, syntax: SyntaxNames.Literal };
    const expression = condition.expressions[condition.syntax || 'Literal'] || '';
    context.bodyDisplay = `<p>${expression}</p>`;
  }
}
